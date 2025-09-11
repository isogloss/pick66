cmake_minimum_required(VERSION 3.20)
project(Pick6CPP)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Platform specific settings
if(WIN32)
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /W4")
    # Link Windows libraries
    set(WIN32_LIBS user32 gdi32 kernel32 comctl32 shell32 ole32)
else()
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -Wall -Wextra")
    # For Linux development/testing - use GTK or Qt (optional)
    find_package(PkgConfig)
    if(PkgConfig_FOUND)
        pkg_check_modules(GTK3 gtk+-3.0)
    endif()
endif()

# Include directories
include_directories(${CMAKE_CURRENT_SOURCE_DIR}/src)

# Source files
set(CORE_SOURCES
    src/core/GameCapture.cpp
    src/core/ProcessDetector.cpp
    src/core/FrameBuffer.cpp
    src/core/Settings.cpp
)

set(GUI_SOURCES
    src/gui/MainWindow.cpp
    src/gui/SettingsDialog.cpp
    src/gui/KeybindManager.cpp
)

set(PROJECTION_SOURCES
    src/projection/ProjectionWindow.cpp
    src/projection/StealthManager.cpp
    src/projection/Renderer.cpp
)

set(MAIN_SOURCES
    src/main.cpp
)

# Create the executable
add_executable(Pick6CPP
    ${MAIN_SOURCES}
    ${CORE_SOURCES}
    ${GUI_SOURCES}
    ${PROJECTION_SOURCES}
)

# Platform specific linking
if(WIN32)
    target_link_libraries(Pick6CPP ${WIN32_LIBS})
    # Hide console window for Windows GUI app
    set_target_properties(Pick6CPP PROPERTIES
        WIN32_EXECUTABLE TRUE
        LINK_FLAGS "/SUBSYSTEM:WINDOWS"
    )
else()
    if(GTK3_FOUND)
        target_link_libraries(Pick6CPP ${GTK3_LIBRARIES})
        target_include_directories(Pick6CPP PRIVATE ${GTK3_INCLUDE_DIRS})
        target_compile_definitions(Pick6CPP PRIVATE -DHAVE_GTK3)
    endif()
endif()

# Create directories for build output
file(MAKE_DIRECTORY ${CMAKE_BINARY_DIR}/bin)
set_target_properties(Pick6CPP PROPERTIES
    RUNTIME_OUTPUT_DIRECTORY ${CMAKE_BINARY_DIR}/bin
)