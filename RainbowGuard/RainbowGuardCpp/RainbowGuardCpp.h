#pragma once

#ifdef RAINBOWGUARDCPP_EXPORTS
#define API __declspec(dllexport)
#else
#define API __declspec(dllimport)
#endif

extern "C" API void* __cdecl GetCreatePixelShader();

typedef void(__cdecl* LogCallback)(const char* msg);
extern "C" API void __cdecl SetLogCallback(LogCallback cb_msg, LogCallback cb_warn, LogCallback cb_err);
