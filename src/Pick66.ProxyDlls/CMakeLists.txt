# Pick66 Proxy DLLs CMake Configuration
cmake_minimum_required(VERSION 3.20)
project(Pick66ProxyDlls)

# Note: This CMake file is designed for Windows builds with MSVC
# For cross-compilation or actual Windows builds, additional configuration is needed

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Include directories
include_directories(Common)

# Find ImGui (would need to be added as submodule or external dependency)
# For now, we'll create placeholder libraries

# Common library
add_library(Pick66Common STATIC
    Common/ProxyCommon.h
    Common/ProxyCommon.cpp
)

# DXGI Proxy DLL
add_library(Pick66DxgiProxy SHARED
    DxgiProxy/DxgiProxy.h
    DxgiProxy/DxgiProxy.cpp
    DxgiProxy/DxgiProxy.def  # Export definition file
)

target_link_libraries(Pick66DxgiProxy Pick66Common)

# D3D11 Proxy DLL (similar structure)
add_library(Pick66D3D11Proxy SHARED
    D3D11Proxy/D3D11Proxy.h
    D3D11Proxy/D3D11Proxy.cpp
    D3D11Proxy/D3D11Proxy.def
)

target_link_libraries(Pick66D3D11Proxy Pick66Common)

# Set output names to match the system DLLs
set_target_properties(Pick66DxgiProxy PROPERTIES OUTPUT_NAME "dxgi")
set_target_properties(Pick66D3D11Proxy PROPERTIES OUTPUT_NAME "d3d11")

# Windows-specific configuration
if(WIN32)
    target_link_libraries(Pick66Common d3d11 dxgi dwmapi)
    target_link_libraries(Pick66DxgiProxy d3d11 dxgi)
    target_link_libraries(Pick66D3D11Proxy d3d11 dxgi)
    
    # Set subsystem to Windows
    set_target_properties(Pick66DxgiProxy PROPERTIES LINK_FLAGS "/SUBSYSTEM:WINDOWS")
    set_target_properties(Pick66D3D11Proxy PROPERTIES LINK_FLAGS "/SUBSYSTEM:WINDOWS")
endif()

# Install targets
install(TARGETS Pick66DxgiProxy Pick66D3D11Proxy
    RUNTIME DESTINATION ProxyDlls
    LIBRARY DESTINATION ProxyDlls
)

# Copy proxy DLLs to launcher output directory
add_custom_command(TARGET Pick66DxgiProxy POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:Pick66DxgiProxy> ${CMAKE_BINARY_DIR}/ProxyDlls/dxgi.dll
)

add_custom_command(TARGET Pick66D3D11Proxy POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy $<TARGET_FILE:Pick66D3D11Proxy> ${CMAKE_BINARY_DIR}/ProxyDlls/d3d11.dll
)