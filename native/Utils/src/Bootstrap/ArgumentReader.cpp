#include <Bootstrap/Argument.hpp>
#include <optional>
#include <string>
#include <unordered_map>

namespace Utils::Bootstrap
{
	std::unordered_map<std::string, Argument> ReadArguments(const int argc, char* argv[])
	{
		std::unordered_map<std::string, Argument> result;
		std::optional<std::string> pending;

		for (int i = 0; i < argc; ++i)
		{
			const std::string &argument = argv[i];

			if (argument.starts_with("-") == false)
			{
				if (pending.has_value())
				{
					std::string name = pending.value();
					result[name] = Argument(name, argument);
					pending.reset();
				}

				continue;
			}

			result[argument] = Argument(argument);
			pending = argument;
		}

		return result;
	}
}
