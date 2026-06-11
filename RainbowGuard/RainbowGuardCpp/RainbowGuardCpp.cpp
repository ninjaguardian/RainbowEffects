#include "RainbowGuardCpp.h"
#include <cstdio>
#include "kiero.hpp"
#include "kiero_d3d11.hpp"

namespace {
	LogCallback logger_msg = nullptr;
	LogCallback logger_warn = nullptr;
	LogCallback logger_err = nullptr;

	/*void* ResolveJmp(void* addr)
	{
		const auto p = static_cast<unsigned char*>(addr);

		if (p[0] != 0xE9)
			return addr; // not JMP

		const int32_t offset = *reinterpret_cast<int32_t*>(p + 1);
		return p + 5 + offset;
	}*/
}

void* GetCreatePixelShader()
{
	kiero::D3D11Output output;
	const auto error = kiero::locate<kiero::Implementation_D3D11>(nullptr, &output);
	if (error != kiero::Error_Nil)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Failed to locate D3D11: %d", error) > 0)
				logger_err(buffer);
		}
		return nullptr;
	}

	if (output.device_methods.size() <= 15)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Device methods size too small: %zu", output.device_methods.size()) > 0)
				logger_err(buffer);
		}
		return nullptr;
	}

	const auto target = output.device_methods[15];

	if (!target)
	{
		if (logger_err)
			logger_err("Failed to locate CreatePixelShader");
		return nullptr;
	}

	return target;
}

void SetLogCallback(const LogCallback cb_msg, const LogCallback cb_warn, const LogCallback cb_err)
{
	logger_msg = cb_msg;
	logger_warn = cb_warn;
	logger_err = cb_err;
}
