// 服务器端：读取固件文件并主动推送给客户端
#include "fw_protocol.hpp"
#ifdef _WIN32
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
typedef SOCKET sock_t;
#define CLOSESOCK closesocket
#define INIT_NET()                             \
    WSADATA wsa{};                             \
    if (WSAStartup(MAKEWORD(2, 2), &wsa) != 0) \
    {                                          \
        std::cerr << "WSAStartup failed\n";    \
        return 0;                              \
    }
#define CLEAN_NET() WSACleanup()
#else
#include <arpa/inet.h>
#include <unistd.h>
typedef int sock_t;
#define CLOSESOCK close
#define INIT_NET() (void)0
#define CLEAN_NET() (void)0
#endif
#include <fstream>
#include <iostream>
#include <thread>

bool send_frame(int sock, const Frame &f)
{
    auto b = f.serialize();
    size_t s = 0;
    while (s < b.size())
    {
        int n = send(sock, b.data() + s, b.size() - s, 0);
        if (n <= 0)
            return false;
        s += n;
    }
    return true;
}
bool wait_ack(int sock, uint16_t id)
{
    std::vector<uint8_t> buf;
    Frame f;
    while (true)
    {
        uint8_t tmp[1024];
        int n = recv(sock, tmp, sizeof(tmp), 0);
        if (n <= 0)
            return false;
        buf.insert(buf.end(), tmp, tmp + n);
        while (Frame::try_parse(buf, f))
        {
            if (f.cmd == Cmd::Ack && f.id == id)
                return true;
        }
    }
}
void session(int c, const std::string &fw)
{
    std::ifstream fin(fw, std::ios::binary);
    if (!fin)
    {
        std::cout << "open fw fail\n";
        close(c);
        return;
    }
    fin.seekg(0, std::ios::end);
    size_t sz = fin.tellg();
    fin.seekg(0);
    uint16_t id = 1;
    Frame start;
    start.cmd = Cmd::StartUpgrade;
    start.id = id++;
    start.data.assign((uint8_t *)&sz, (uint8_t *)&sz + 4);
    send_frame(c, start);
    if (!wait_ack(c, start.id))
    {
        close(c);
        return;
    }
    const size_t CHK = 1280;
    std::vector<uint8_t> buf(CHK);
    while (fin)
    {
        fin.read((char *)buf.data(), CHK);
        size_t r = fin.gcount();
        if (r == 0)
            break;
        Frame pkt;
        pkt.cmd = Cmd::TransferChunk;
        pkt.id = id++;
        pkt.data.assign(buf.begin(), buf.begin() + r);
        send_frame(c, pkt);
        if (!wait_ack(c, pkt.id))
        {
            close(c);
            return;
        }
    }
    Frame end;
    end.cmd = Cmd::EndUpgrade;
    end.id = id;
    send_frame(c, end);
    wait_ack(c, end.id);
    close(c);
}
int main(int argc, char *argv[])
{
    if (argc < 3)
    {
        std::cout << "Usage: " << argv[0] << " <port> <firmware.bin>\n";
        return 0;
    }
    int port = std::stoi(argv[1]);
    std::string fw = argv[2];
    int s = socket(AF_INET, SOCK_STREAM, 0);
    int opt = 1;
    setsockopt(s, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt));
    sockaddr_in addr{AF_INET, htons(port), INADDR_ANY};
    bind(s, (sockaddr *)&addr, sizeof(addr));
    listen(s, 5);
    std::cout << "[Server] listen " << port << "\n";
    while (true)
    {
        int c = accept(s, nullptr, nullptr);
        std::thread(session, c, fw).detach();
    }
}