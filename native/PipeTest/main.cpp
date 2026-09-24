#include <Bootstrap/Argument.hpp>
#include <Bootstrap/ArgumentReader.hpp>
#include <chrono>
#include <exception>
#include <format>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>
#include <vector>
#include <windows.h>

const int BUFFER_SIZE = 1024;
HANDLE pipe;

static void Connect(const std::string& pipe_name)
{
    pipe = CreateNamedPipeA(
        std::format(R"(\\.\pipe\{})", pipe_name).c_str(),
        PIPE_ACCESS_DUPLEX,
        PIPE_TYPE_BYTE | PIPE_READMODE_BYTE | PIPE_WAIT,
        1,
        BUFFER_SIZE,
        BUFFER_SIZE,
        0,
        nullptr);

    BOOL success = ConnectNamedPipe(pipe, nullptr);

    if (success)
    {
        std::cout << "Connected to pipe " << pipe_name << '\n';
        return;
    }

    throw std::runtime_error("Failed to connect");
}

static BOOL WriteLine(const std::string& message)
{
    std::string data = message + '\n';
    std::cout << "Sending: " << data;
    return WriteFile(pipe, data.data(), static_cast<DWORD>(data.size()), nullptr, nullptr);
}

static void ReadLine(const std::string& expected)
{
    std::vector<char> buffer(expected.size());

    DWORD bytesRead;
    BOOL success = ReadFile(pipe, buffer.data(), expected.size(), &bytesRead, nullptr);

    std::string actual(buffer.data(), bytesRead);
    std::cout << "Received: " << actual << '\n';

    if (expected == actual) return;
    throw std::runtime_error(std::format("Expected: {}\n\nBut Was:  {}", expected, actual));
}

int main(int argc, char* argv[])
{
    try
    {
        auto arguments = Utils::Bootstrap::ReadArguments(argc, argv);

        if (arguments.size() == 0)
        {
            std::cerr << "No arguments found" << '\n';
            return 1;
        }
        
        Utils::Bootstrap::Argument reads = arguments["--reads"];
        Utils::Bootstrap::Argument sends = arguments["--sends"];

        Connect(arguments["--pipe-name"].RequireValue());

        if (reads.HasValue()) ReadLine(reads.RequireValue());
        if (sends.HasValue()) WriteLine(sends.RequireValue());

        CloseHandle(pipe);
        return 0;
    }
    catch (const std::exception& ex)
    {
        std::cerr << ex.what() << '\n';
        std::this_thread::sleep_for(std::chrono::milliseconds(10000));
        return 1;
    }
}
