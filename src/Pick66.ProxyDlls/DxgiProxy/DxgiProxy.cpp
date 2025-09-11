#include "DxgiProxy.h"
#include "../Common/ProxyCommon.h"
#include <filesystem>

namespace Pick66::DxgiProxy
{
    static HMODULE s_originalDxgi = nullptr;
    static bool s_initialized = false;

    // Function pointers for original DXGI functions
    typedef HRESULT(WINAPI* CreateDXGIFactory_t)(REFIID, void**);
    typedef HRESULT(WINAPI* CreateDXGIFactory1_t)(REFIID, void**);
    typedef HRESULT(WINAPI* CreateDXGIFactory2_t)(UINT, REFIID, void**);

    static CreateDXGIFactory_t s_originalCreateDXGIFactory = nullptr;
    static CreateDXGIFactory1_t s_originalCreateDXGIFactory1 = nullptr;
    static CreateDXGIFactory2_t s_originalCreateDXGIFactory2 = nullptr;

    // DXGISwapChainProxy implementation
    DXGISwapChainProxy::DXGISwapChainProxy(IDXGISwapChain* original)
        : m_original(original), m_refCount(1), m_d3d11DeviceObtained(false)
    {
        if (m_original)
            m_original->AddRef();
    }

    DXGISwapChainProxy::~DXGISwapChainProxy()
    {
        if (m_original)
            m_original->Release();
    }

    STDMETHODIMP DXGISwapChainProxy::QueryInterface(REFIID riid, void** ppvObject)
    {
        if (riid == __uuidof(IUnknown) || riid == __uuidof(IDXGISwapChain))
        {
            AddRef();
            *ppvObject = this;
            return S_OK;
        }
        
        return m_original->QueryInterface(riid, ppvObject);
    }

    STDMETHODIMP_(ULONG) DXGISwapChainProxy::AddRef()
    {
        return InterlockedIncrement(&m_refCount);
    }

    STDMETHODIMP_(ULONG) DXGISwapChainProxy::Release()
    {
        ULONG count = InterlockedDecrement(&m_refCount);
        if (count == 0)
            delete this;
        return count;
    }

    STDMETHODIMP DXGISwapChainProxy::Present(UINT SyncInterval, UINT Flags)
    {
        // This is the key hook point for frame capture and overlay
        auto& hookManager = D3D11HookManager::Instance();
        
        // Try to get D3D11 device on first present
        if (!m_d3d11DeviceObtained)
        {
            ID3D11Device* device = nullptr;
            if (SUCCEEDED(m_original->GetDevice(__uuidof(ID3D11Device), (void**)&device)) && device)
            {
                ID3D11DeviceContext* context = nullptr;
                device->GetImmediateContext(&context);
                
                if (context)
                {
                    hookManager.OnDeviceCreated(device, context);
                    context->Release();
                    m_d3d11DeviceObtained = true;
                }
                device->Release();
            }
        }

        // Call our pre-present hook (for overlay rendering)
        hookManager.OnBeforePresent(m_original);

        // Call original Present
        HRESULT result = m_original->Present(SyncInterval, Flags);

        // Call our post-present hook
        hookManager.OnAfterPresent(m_original);

        return result;
    }

    STDMETHODIMP DXGISwapChainProxy::GetBuffer(UINT Buffer, REFIID riid, void** ppSurface)
    {
        FORWARD_INTERFACE_METHOD(GetBuffer, Buffer, riid, ppSurface);
    }

    STDMETHODIMP DXGISwapChainProxy::SetFullscreenState(BOOL Fullscreen, IDXGIOutput* pTarget)
    {
        FORWARD_INTERFACE_METHOD(SetFullscreenState, Fullscreen, pTarget);
    }

    STDMETHODIMP DXGISwapChainProxy::GetFullscreenState(BOOL* pFullscreen, IDXGIOutput** ppTarget)
    {
        FORWARD_INTERFACE_METHOD(GetFullscreenState, pFullscreen, ppTarget);
    }

    STDMETHODIMP DXGISwapChainProxy::GetDesc(DXGI_SWAP_CHAIN_DESC* pDesc)
    {
        FORWARD_INTERFACE_METHOD(GetDesc, pDesc);
    }

    STDMETHODIMP DXGISwapChainProxy::ResizeBuffers(UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags)
    {
        FORWARD_INTERFACE_METHOD(ResizeBuffers, BufferCount, Width, Height, NewFormat, SwapChainFlags);
    }

    STDMETHODIMP DXGISwapChainProxy::ResizeTarget(const DXGI_MODE_DESC* pNewTargetParameters)
    {
        FORWARD_INTERFACE_METHOD(ResizeTarget, pNewTargetParameters);
    }

    STDMETHODIMP DXGISwapChainProxy::GetContainingOutput(IDXGIOutput** ppOutput)
    {
        FORWARD_INTERFACE_METHOD(GetContainingOutput, ppOutput);
    }

    STDMETHODIMP DXGISwapChainProxy::GetFrameStatistics(DXGI_FRAME_STATISTICS* pStats)
    {
        FORWARD_INTERFACE_METHOD(GetFrameStatistics, pStats);
    }

    STDMETHODIMP DXGISwapChainProxy::GetLastPresentCount(UINT* pLastPresentCount)
    {
        FORWARD_INTERFACE_METHOD(GetLastPresentCount, pLastPresentCount);
    }

    STDMETHODIMP DXGISwapChainProxy::SetPrivateData(REFGUID Name, UINT DataSize, const void* pData)
    {
        FORWARD_INTERFACE_METHOD(SetPrivateData, Name, DataSize, pData);
    }

    STDMETHODIMP DXGISwapChainProxy::SetPrivateDataInterface(REFGUID Name, const IUnknown* pUnknown)
    {
        FORWARD_INTERFACE_METHOD(SetPrivateDataInterface, Name, pUnknown);
    }

    STDMETHODIMP DXGISwapChainProxy::GetPrivateData(REFGUID Name, UINT* pDataSize, void* pData)
    {
        FORWARD_INTERFACE_METHOD(GetPrivateData, Name, pDataSize, pData);
    }

    STDMETHODIMP DXGISwapChainProxy::GetParent(REFIID riid, void** ppParent)
    {
        FORWARD_INTERFACE_METHOD(GetParent, riid, ppParent);
    }

    STDMETHODIMP DXGISwapChainProxy::GetDevice(REFIID riid, void** ppDevice)
    {
        FORWARD_INTERFACE_METHOD(GetDevice, riid, ppDevice);
    }

    // DXGIFactoryProxy implementation
    DXGIFactoryProxy::DXGIFactoryProxy(IDXGIFactory* original)
        : m_original(original), m_refCount(1)
    {
        if (m_original)
            m_original->AddRef();
    }

    DXGIFactoryProxy::~DXGIFactoryProxy()
    {
        if (m_original)
            m_original->Release();
    }

    STDMETHODIMP DXGIFactoryProxy::QueryInterface(REFIID riid, void** ppvObject)
    {
        if (riid == __uuidof(IUnknown) || riid == __uuidof(IDXGIFactory))
        {
            AddRef();
            *ppvObject = this;
            return S_OK;
        }
        
        return m_original->QueryInterface(riid, ppvObject);
    }

    STDMETHODIMP_(ULONG) DXGIFactoryProxy::AddRef()
    {
        return InterlockedIncrement(&m_refCount);
    }

    STDMETHODIMP_(ULONG) DXGIFactoryProxy::Release()
    {
        ULONG count = InterlockedDecrement(&m_refCount);
        if (count == 0)
            delete this;
        return count;
    }

    STDMETHODIMP DXGIFactoryProxy::CreateSwapChain(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain)
    {
        HRESULT result = m_original->CreateSwapChain(pDevice, pDesc, ppSwapChain);
        
        if (SUCCEEDED(result) && ppSwapChain && *ppSwapChain)
        {
            // Wrap the swap chain in our proxy
            *ppSwapChain = new DXGISwapChainProxy(*ppSwapChain);
            PICK66_LOG(L"SwapChain created and wrapped in proxy");
        }
        
        return result;
    }

    STDMETHODIMP DXGIFactoryProxy::EnumAdapters(UINT Adapter, IDXGIAdapter** ppAdapter)
    {
        FORWARD_INTERFACE_METHOD(EnumAdapters, Adapter, ppAdapter);
    }

    STDMETHODIMP DXGIFactoryProxy::MakeWindowAssociation(HWND WindowHandle, UINT Flags)
    {
        FORWARD_INTERFACE_METHOD(MakeWindowAssociation, WindowHandle, Flags);
    }

    STDMETHODIMP DXGIFactoryProxy::GetWindowAssociation(HWND* pWindowHandle)
    {
        FORWARD_INTERFACE_METHOD(GetWindowAssociation, pWindowHandle);
    }

    STDMETHODIMP DXGIFactoryProxy::CreateSoftwareAdapter(HMODULE Module, IDXGIAdapter** ppAdapter)
    {
        FORWARD_INTERFACE_METHOD(CreateSoftwareAdapter, Module, ppAdapter);
    }

    STDMETHODIMP DXGIFactoryProxy::SetPrivateData(REFGUID Name, UINT DataSize, const void* pData)
    {
        FORWARD_INTERFACE_METHOD(SetPrivateData, Name, DataSize, pData);
    }

    STDMETHODIMP DXGIFactoryProxy::SetPrivateDataInterface(REFGUID Name, const IUnknown* pUnknown)
    {
        FORWARD_INTERFACE_METHOD(SetPrivateDataInterface, Name, pUnknown);
    }

    STDMETHODIMP DXGIFactoryProxy::GetPrivateData(REFGUID Name, UINT* pDataSize, void* pData)
    {
        FORWARD_INTERFACE_METHOD(GetPrivateData, Name, pDataSize, pData);
    }

    STDMETHODIMP DXGIFactoryProxy::GetParent(REFIID riid, void** ppParent)
    {
        FORWARD_INTERFACE_METHOD(GetParent, riid, ppParent);
    }

    // Exported DXGI functions
    extern "C"
    {
        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory(REFIID riid, void** ppFactory)
        {
            if (!s_originalCreateDXGIFactory)
                return E_FAIL;

            HRESULT result = s_originalCreateDXGIFactory(riid, ppFactory);
            
            if (SUCCEEDED(result) && ppFactory && *ppFactory)
            {
                IDXGIFactory* originalFactory = static_cast<IDXGIFactory*>(*ppFactory);
                *ppFactory = new DXGIFactoryProxy(originalFactory);
                originalFactory->Release(); // Proxy holds its own reference
                PICK66_LOG(L"DXGI Factory created and wrapped in proxy");
            }
            
            return result;
        }

        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory1(REFIID riid, void** ppFactory)
        {
            if (!s_originalCreateDXGIFactory1)
                return E_FAIL;

            HRESULT result = s_originalCreateDXGIFactory1(riid, ppFactory);
            
            if (SUCCEEDED(result) && ppFactory && *ppFactory)
            {
                IDXGIFactory* originalFactory = static_cast<IDXGIFactory*>(*ppFactory);
                *ppFactory = new DXGIFactoryProxy(originalFactory);
                originalFactory->Release(); // Proxy holds its own reference
                PICK66_LOG(L"DXGI Factory1 created and wrapped in proxy");
            }
            
            return result;
        }

        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory2(UINT Flags, REFIID riid, void** ppFactory)
        {
            if (!s_originalCreateDXGIFactory2)
                return E_FAIL;

            HRESULT result = s_originalCreateDXGIFactory2(Flags, riid, ppFactory);
            
            if (SUCCEEDED(result) && ppFactory && *ppFactory)
            {
                IDXGIFactory* originalFactory = static_cast<IDXGIFactory*>(*ppFactory);
                *ppFactory = new DXGIFactoryProxy(originalFactory);
                originalFactory->Release(); // Proxy holds its own reference
                PICK66_LOG(L"DXGI Factory2 created and wrapped in proxy");
            }
            
            return result;
        }
    }

    // DLL management functions
    namespace DllManagement
    {
        bool Initialize()
        {
            if (s_initialized)
                return true;

            // Load the original DXGI.dll from System32
            wchar_t systemPath[MAX_PATH];
            GetSystemDirectoryW(systemPath, MAX_PATH);
            std::wstring dxgiPath = std::wstring(systemPath) + L"\\dxgi.dll";

            s_originalDxgi = LoadLibraryW(dxgiPath.c_str());
            if (!s_originalDxgi)
            {
                PICK66_LOG_ERROR(L"Failed to load original dxgi.dll");
                return false;
            }

            // Get function pointers
            s_originalCreateDXGIFactory = (CreateDXGIFactory_t)GetProcAddress(s_originalDxgi, "CreateDXGIFactory");
            s_originalCreateDXGIFactory1 = (CreateDXGIFactory1_t)GetProcAddress(s_originalDxgi, "CreateDXGIFactory1");
            s_originalCreateDXGIFactory2 = (CreateDXGIFactory2_t)GetProcAddress(s_originalDxgi, "CreateDXGIFactory2");

            if (!s_originalCreateDXGIFactory)
            {
                PICK66_LOG_ERROR(L"Failed to get CreateDXGIFactory from original dxgi.dll");
                return false;
            }

            // Initialize D3D11 hook manager
            D3D11HookManager::Instance().Initialize();

            s_initialized = true;
            PICK66_LOG(L"DXGI Proxy initialized successfully");
            return true;
        }

        void Shutdown()
        {
            if (!s_initialized)
                return;

            D3D11HookManager::Instance().Shutdown();

            if (s_originalDxgi)
            {
                FreeLibrary(s_originalDxgi);
                s_originalDxgi = nullptr;
            }

            s_originalCreateDXGIFactory = nullptr;
            s_originalCreateDXGIFactory1 = nullptr;
            s_originalCreateDXGIFactory2 = nullptr;
            s_initialized = false;

            PICK66_LOG(L"DXGI Proxy shutdown");
        }

        HMODULE GetOriginalDXGI()
        {
            return s_originalDxgi;
        }
    }
}

// DLL entry point
BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)
{
    switch (ul_reason_for_call)
    {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        return Pick66::DxgiProxy::DllManagement::Initialize();
        
    case DLL_PROCESS_DETACH:
        Pick66::DxgiProxy::DllManagement::Shutdown();
        break;
    }
    return TRUE;
}