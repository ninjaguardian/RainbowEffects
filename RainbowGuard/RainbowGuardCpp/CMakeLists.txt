cmake_minimum_required(VERSION 3.16)

project(RainbowGuardCpp LANGUAGES CXX)

add_library(RainbowGuardCpp SHARED
    RainbowGuardCpp.cpp
    RainbowGuardCpp.h
)

target_compile_definitions(RainbowGuardCpp PRIVATE RAINBOWGUARDCPP_EXPORTS)
target_compile_features(RainbowGuardCpp PRIVATE cxx_std_23)

include(FetchContent)

set(KIERO_BUILD_D3D11 ON CACHE BOOL "" FORCE)
set(KIERO_BUILD_D3D9  OFF CACHE BOOL "" FORCE)
set(KIERO_BUILD_D3D10 OFF CACHE BOOL "" FORCE)
set(KIERO_BUILD_D3D12 OFF CACHE BOOL "" FORCE)
set(KIERO_BUILD_OPENGL OFF CACHE BOOL "" FORCE)
set(KIERO_BUILD_VULKAN OFF CACHE BOOL "" FORCE)

FetchContent_Declare(
  kiero
  GIT_REPOSITORY https://github.com/ninjaguardian/kiero2.git
  GIT_TAG test2
)

FetchContent_MakeAvailable(kiero)

target_link_libraries(RainbowGuardCpp PRIVATE kiero::kiero)
