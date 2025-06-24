#pragma once
#include <cstdint>
#include <vector>
#include <cstring>

// Magic "SCZN" 小端
constexpr uint32_t FW_MAGIC = 0x53435A4E;

// 命令字
enum class Cmd : uint8_t
{
    StartUpgrade   = 0x01,
    TransferChunk  = 0x02,
    EndUpgrade     = 0x03,
    Ack            = 0x04,
    Nack           = 0x05
};

// 简易 CRC32 (多项式 0xEDB88320)
inline uint32_t crc32(const uint8_t* buf, size_t len)
{
    static uint32_t tbl[256];
    static bool inited = false;
    if (!inited)
    {
        for (uint32_t i = 0; i < 256; ++i)
        {
            uint32_t c = i;
            for (int k = 0; k < 8; ++k)
                c = (c & 1) ? (0xEDB88320U ^ (c >> 1)) : (c >> 1);
            tbl[i] = c;
        }
        inited = true;
    }
    uint32_t crc = 0xFFFFFFFFU;
    for (size_t i = 0; i < len; ++i)
        crc = tbl[(crc ^ buf[i]) & 0xFF] ^ (crc >> 8);
    return crc ^ 0xFFFFFFFFU;
}

struct Frame
{
    uint32_t magic = FW_MAGIC;
    uint8_t  ver   = 1;
    Cmd      cmd   = Cmd::Ack;
    uint16_t id    = 0;
    std::vector<uint8_t> data; // 可选

    std::vector<uint8_t> serialize() const
    {
        uint32_t len = static_cast<uint32_t>(data.size());
        std::vector<uint8_t> buf;
        buf.reserve(4 + 1 + 1 + 2 + 4 + len + 4);

        auto put32 = [&](uint32_t v){ buf.push_back(v & 0xFF); buf.push_back((v>>8)&0xFF); buf.push_back((v>>16)&0xFF); buf.push_back((v>>24)&0xFF);} ;
        auto put16 = [&](uint16_t v){ buf.push_back(v & 0xFF); buf.push_back((v>>8)&0xFF);} ;

        put32(magic);
        buf.push_back(ver);
        buf.push_back(static_cast<uint8_t>(cmd));
        put16(id);
        put32(len);
        buf.insert(buf.end(), data.begin(), data.end());
        uint32_t crc = crc32(buf.data(), buf.size());
        put32(crc);
        return buf;
    }

    // 尝试从 in (可包含多帧) 拆出一帧到 out
    static bool try_parse(std::vector<uint8_t>& in, Frame& out)
    {
        while (in.size() >= 16)
        {
            // 校验魔数
            uint32_t mg = in[0] | (in[1]<<8) | (in[2]<<16) | (in[3]<<24);
            if (mg != FW_MAGIC) { in.erase(in.begin()); continue; }

            uint8_t ver = in[4];
            uint8_t cmd = in[5];
            uint16_t id = in[6] | (in[7]<<8);
            uint32_t len = in[8] | (in[9]<<8) | (in[10]<<16) | (in[11]<<24);
            size_t total = 12 + len + 4;
            if (in.size() < total) return false; // 未收齐

            uint32_t crcRecv = in[total-4] | (in[total-3]<<8) | (in[total-2]<<16) | (in[total-1]<<24);
            if (crc32(in.data(), total-4)!=crcRecv) { in.erase(in.begin()); continue; }

            out.magic = mg; out.ver = ver; out.cmd = static_cast<Cmd>(cmd); out.id = id;
            out.data.assign(in.begin()+12, in.begin()+12+len);
            in.erase(in.begin(), in.begin()+total);
            return true;
        }
        return false;
    }
}; 