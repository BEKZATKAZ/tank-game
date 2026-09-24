#include <Bootstrap/Argument.hpp>
#include <format>
#include <stdexcept>
#include <string>

namespace Utils::Bootstrap
{
	bool Argument::HasValue() const
	{
		return value.has_value();
	}

	std::string Argument::RequireValue() const
	{
		if (value.has_value()) return value.value();
		throw std::runtime_error(std::format("Argument {} has no value", name));
	}
}
