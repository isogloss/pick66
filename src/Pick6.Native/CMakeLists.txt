cmake_minimum_required(VERSION 3.15)
project(Pick6Native VERSION 1.0.0 LANGUAGES CXX)

# Set C++ standard
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Define the DLL
add_library(Pick6Native SHARED
    DllMain.cpp
    Pick6Injector.cpp
    Pick6Export.cpp
    Pick6Injector.h
    Pick6Export.h
)

# Set output name
set_target_properties(Pick6Native PROPERTIES
    OUTPUT_NAME "Pick6Native"
    PREFIX ""
)

# Define export macro
target_compile_definitions(Pick6Native PRIVATE PICK6NATIVE_EXPORTS)

# Link against required Windows libraries
target_link_libraries(Pick6Native PRIVATE
    kernel32
    advapi32
    psapi
)

# Set warning level
if(MSVC)
    target_compile_options(Pick6Native PRIVATE /W4)
    # Optimize for minimal size in Release
    target_compile_options(Pick6Native PRIVATE $<$<CONFIG:Release>:/O1 /Oi>)
    # Enable static runtime to minimize dependencies
    set_property(TARGET Pick6Native PROPERTY
        MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
else()
    target_compile_options(Pick6Native PRIVATE -Wall -Wextra -pedantic)
endif()

# Installation
install(TARGETS Pick6Native
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)

# Install headers for development
install(FILES Pick6Export.h Pick6Injector.h
    DESTINATION include/Pick6Native
)

# Create a simple test executable (optional)
option(BUILD_TESTS "Build test executables" ON)
if(BUILD_TESTS)
    add_executable(Pick6Test test/TestMain.cpp)
    target_link_libraries(Pick6Test PRIVATE Pick6Native)
    target_include_directories(Pick6Test PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})
endif()
