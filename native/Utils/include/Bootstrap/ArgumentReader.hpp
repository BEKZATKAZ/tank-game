#pragma once

#include "Argument.hpp"
#include <string>
#include <unordered_map>

namespace Utils::Bootstrap
{
	std::unordered_map<std::string, Argument> ReadArguments(const int argc, char* argv[]);
}
