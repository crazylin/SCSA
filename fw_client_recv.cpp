#include "fw_protocol.hpp"

#ifdef _WIN32
    #define _WINSOCK_DEPRECATED_NO_WARNINGS
    #include <winsock2.h>
    #include <ws2tcpip.h>
    #pragma comment(lib, "ws2_32.lib")
    typedef SOCKET sock_t;
    #define CLOSESOCK(s)  closesocket(s)
    #define INIT_NET()    \
        { WSADATA wsa{}; if (WSAStartup(MAKEWORD(2,2), &wsa)!=0){ \
            std::cerr<<"WSAStartup failed\\n"; return 0; } }
    #define CLEAN_NET()   WSACleanup()
#else
    #include <arpa/inet.h>
    #include <unistd.h>
    typedef int sock_t;
    #define CLOSESOCK(s)  close(s)
    #define INIT_NET()    (void)0
    #define CLEAN_NET()   (void)0
#endif

#include <iostream>
#include <fstream>
#include <vector>
#include <thread>

bool send_ack(SOCKET sock, uint16_t id, bool ok = true)
{
    Frame rsp;  rsp.cmd = ok ? Cmd::Ack : Cmd::Nack;  rsp.id = id;
    auto buf = rsp.serialize();
    size_t sent = 0;
    while (sent < buf.size())
    {
        int n = send(sock,
                     reinterpret_cast<const char*>(buf.data()) + sent,
                     static_cast<int>(buf.size() - sent),
                     0);
        if (n <= 0) return false;
        sent += n;
    }
    return true;
}

int main(int argc,char* argv[])
{
    if(argc<4){ std::cout<<"Usage: "<<argv[0]<<" <server_ip> <port> <save.bin>\n"; return 0; }
    std::string ip=argv[1]; int port=std::stoi(argv[2]); std::string outfile=argv[3];

    int sock=socket(AF_INET,SOCK_STREAM,0);
    sockaddr_in addr{}; addr.sin_family=AF_INET; addr.sin_port=htons(port); inet_pton(AF_INET,ip.c_str(),&addr.sin_addr);
    if(connect(sock,(sockaddr*)&addr,sizeof(addr))<0){ perror("connect"); return 0; }
    std::cout<<"[Client] connected\n";

    std::vector<uint8_t> recvBuf; Frame f; std::ofstream fout(outfile,std::ios::binary);
    size_t expectSize=0; size_t received=0;

    while(true)
    {
        uint8_t tmp[2048]; int n = recv(sock,
                                         reinterpret_cast<char*>(tmp),
                                         sizeof(tmp),
                                         0);
        if(n<=0) break; recvBuf.insert(recvBuf.end(),tmp,tmp+n);
        while(Frame::try_parse(recvBuf,f))
        {
            switch(f.cmd)
            {
                case Cmd::StartUpgrade:
                    if(f.data.size()==4){ expectSize = f.data[0] | (f.data[1]<<8) | (f.data[2]<<16) | (f.data[3]<<24); received=0; fout.seekp(0); }
                    send_ack(sock,f.id); break;
                case Cmd::TransferChunk:
                    fout.write((char*)f.data.data(), f.data.size()); received += f.data.size();
                    send_ack(sock,f.id); break;
                case Cmd::EndUpgrade:
                    send_ack(sock,f.id);
                    std::cout<<"[Client] firmware received "<<received<<" / "<<expectSize<<" bytes\n";
                    goto END;
                default:
                    send_ack(sock,f.id,false); break;
            }
        }
    }
END:
    fout.close();
    CLOSESOCK(sock);
    CLEAN_NET();
    return 0;
} 