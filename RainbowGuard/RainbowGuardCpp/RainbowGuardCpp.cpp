//#include <Windows.h>
#include "RainbowGuardCpp.h"
#include <cstdio>
#include <cstdint>
//#include <dxgi.h>
//#include <MinHook.h>
#include "kiero.hpp"
#include "kiero_d3d11.hpp"

namespace {
	LogCallback logger_msg = nullptr;
	LogCallback logger_warn = nullptr;
	LogCallback logger_err = nullptr;
}

//typedef HRESULT(__stdcall* PresentFn)(IDXGISwapChain*, UINT, UINT);
//
//PresentFn oPresent = nullptr;
//
//HRESULT __stdcall hkPresent(IDXGISwapChain* sc, UINT sync, UINT flags)
//{
//	if (logger_msg)
//		logger_msg("Present called\n");
//    return oPresent(sc, sync, flags);
//}

void* ResolveJmp(void* addr)
{
	const auto p = static_cast<unsigned char*>(addr);

	if (p[0] != 0xE9)
		return addr; // not JMP

	const int32_t offset = *reinterpret_cast<int32_t*>(p + 1);
	return p + 5 + offset;
}

void* InitMod()
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

	if (output.swapchain_methods.size() <= 8)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Swapchain size too small: %zu", output.swapchain_methods.size()) > 0)
				logger_err(buffer);
		}
		return nullptr;
	}

	const auto present = output.swapchain_methods[8];

	if (!present)
	{
		if (logger_err)
			logger_err("Failed to locate the Present method");
		return nullptr;
	}

	return ResolveJmp(present);
}

/*
int InitMod()
{
	auto status = MH_Initialize();
	if (status != MH_OK)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Failed to initialize MinHook: %d", status) > 0)
				logger_err(buffer);
		}
		return status;
	}

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
		return error;
	}

	if (output.swapchain_methods.size() <= 8)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Swapchain size too small: %zu", output.swapchain_methods.size()) > 0)
				logger_err(buffer);
		}
		return -1;
	}

	const auto target = reinterpret_cast<PresentFn>(output.swapchain_methods[8]);

	if (!target)
	{
		if (logger_err)
			logger_err("Failed to locate the Present method");
		return -2;
	}

	status = MH_CreateHook(
		reinterpret_cast<LPVOID>(target),
		reinterpret_cast<LPVOID>(&hkPresent),
		reinterpret_cast<LPVOID*>(&oPresent)
	);
    if (status != MH_OK)
	{
        if (logger_err)
        {
            char buffer[256];
            if (sprintf_s(buffer, sizeof(buffer),
                "Failed to create hook: %d", status) > 0)
                logger_err(buffer);
        }
        return status;
    }

	status = MH_EnableHook(reinterpret_cast<LPVOID>(target));
    if (status != MH_OK)
	{
        if (logger_err)
        {
            char buffer[256];
            if (sprintf_s(buffer, sizeof(buffer),
                "Failed to enable hook: %d", status) > 0)
                logger_err(buffer);
        }
        return status;
    }

	return kiero::Error_Nil;
}

int DeInitMH()
{
	const auto status = MH_DisableHook(MH_ALL_HOOKS);
	if (status != MH_OK && status != MH_ERROR_NOT_CREATED && logger_warn)
	{
		char buffer[256];
		if (sprintf_s(buffer, sizeof(buffer),
			"Failed to disable hooks: %d", status) > 0)
			logger_warn(buffer);
	}
	const auto uninit_status = MH_Uninitialize();
	if (uninit_status != MH_OK)
	{
		if (logger_err)
		{
			char buffer[256];
			if (sprintf_s(buffer, sizeof(buffer),
				"Failed to uninitialize MinHook: %d", uninit_status) > 0)
				logger_err(buffer);
		}
		return uninit_status;
	}
	return status;
}

int DeInitMod()
{
	const int err = DeInitMH();
	SetLogCallback(nullptr, nullptr, nullptr);
	return err;
}
*/

void SetLogCallback(const LogCallback cb_msg, const LogCallback cb_warn, const LogCallback cb_err)
{
	logger_msg = cb_msg;
	logger_warn = cb_warn;
	logger_err = cb_err;
}
