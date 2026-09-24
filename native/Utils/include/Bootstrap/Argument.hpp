#pragma once

#include <optional>
#include <string>

namespace Utils::Bootstrap
{
	class Argument
	{
	public:
		Argument() = default;

		Argument(std::string name, std::optional<std::string> value = std::nullopt) :
			name(name), value(value) {}

		bool HasValue() const;
		std::string RequireValue() const;

	private:
		std::string name;
		std::optional<std::string> value;
	};
}
