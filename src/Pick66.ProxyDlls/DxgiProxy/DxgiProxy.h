#pragma once

#include "../Common/ProxyCommon.h"
#include <dxgi.h>
#include <d3d11.h>

namespace Pick66::DxgiProxy
{
    /// <summary>
    /// Proxy implementation for IDXGISwapChain
    /// </summary>
    class DXGISwapChainProxy : public IDXGISwapChain
    {
    public:
        DXGISwapChainProxy(IDXGISwapChain* original);
        virtual ~DXGISwapChainProxy();

        // IUnknown methods
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override;
        STDMETHOD_(ULONG, AddRef)() override;
        STDMETHOD_(ULONG, Release)() override;

        // IDXGIObject methods
        STDMETHOD(SetPrivateData)(REFGUID Name, UINT DataSize, const void* pData) override;
        STDMETHOD(SetPrivateDataInterface)(REFGUID Name, const IUnknown* pUnknown) override;
        STDMETHOD(GetPrivateData)(REFGUID Name, UINT* pDataSize, void* pData) override;
        STDMETHOD(GetParent)(REFIID riid, void** ppParent) override;

        // IDXGIDeviceSubObject methods
        STDMETHOD(GetDevice)(REFIID riid, void** ppDevice) override;

        // IDXGISwapChain methods
        STDMETHOD(Present)(UINT SyncInterval, UINT Flags) override;
        STDMETHOD(GetBuffer)(UINT Buffer, REFIID riid, void** ppSurface) override;
        STDMETHOD(SetFullscreenState)(BOOL Fullscreen, IDXGIOutput* pTarget) override;
        STDMETHOD(GetFullscreenState)(BOOL* pFullscreen, IDXGIOutput** ppTarget) override;
        STDMETHOD(GetDesc)(DXGI_SWAP_CHAIN_DESC* pDesc) override;
        STDMETHOD(ResizeBuffers)(UINT BufferCount, UINT Width, UINT Height, DXGI_FORMAT NewFormat, UINT SwapChainFlags) override;
        STDMETHOD(ResizeTarget)(const DXGI_MODE_DESC* pNewTargetParameters) override;
        STDMETHOD(GetContainingOutput)(IDXGIOutput** ppOutput) override;
        STDMETHOD(GetFrameStatistics)(DXGI_FRAME_STATISTICS* pStats) override;
        STDMETHOD(GetLastPresentCount)(UINT* pLastPresentCount) override;

    private:
        IDXGISwapChain* m_original;
        ULONG m_refCount;
        bool m_d3d11DeviceObtained;
    };

    /// <summary>
    /// Proxy implementation for IDXGIFactory
    /// </summary>
    class DXGIFactoryProxy : public IDXGIFactory
    {
    public:
        DXGIFactoryProxy(IDXGIFactory* original);
        virtual ~DXGIFactoryProxy();

        // IUnknown methods
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override;
        STDMETHOD_(ULONG, AddRef)() override;
        STDMETHOD_(ULONG, Release)() override;

        // IDXGIObject methods
        STDMETHOD(SetPrivateData)(REFGUID Name, UINT DataSize, const void* pData) override;
        STDMETHOD(SetPrivateDataInterface)(REFGUID Name, const IUnknown* pUnknown) override;
        STDMETHOD(GetPrivateData)(REFGUID Name, UINT* pDataSize, void* pData) override;
        STDMETHOD(GetParent)(REFIID riid, void** ppParent) override;

        // IDXGIFactory methods
        STDMETHOD(EnumAdapters)(UINT Adapter, IDXGIAdapter** ppAdapter) override;
        STDMETHOD(MakeWindowAssociation)(HWND WindowHandle, UINT Flags) override;
        STDMETHOD(GetWindowAssociation)(HWND* pWindowHandle) override;
        STDMETHOD(CreateSwapChain)(IUnknown* pDevice, DXGI_SWAP_CHAIN_DESC* pDesc, IDXGISwapChain** ppSwapChain) override;
        STDMETHOD(CreateSoftwareAdapter)(HMODULE Module, IDXGIAdapter** ppAdapter) override;

    private:
        IDXGIFactory* m_original;
        ULONG m_refCount;
    };

    /// <summary>
    /// Module functions for the DXGI proxy DLL
    /// </summary>
    extern "C"
    {
        // DXGI exported functions
        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory(REFIID riid, void** ppFactory);
        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory1(REFIID riid, void** ppFactory);
        __declspec(dllexport) HRESULT WINAPI CreateDXGIFactory2(UINT Flags, REFIID riid, void** ppFactory);
    }

    /// <summary>
    /// DLL management functions
    /// </summary>
    namespace DllManagement
    {
        bool Initialize();
        void Shutdown();
        HMODULE GetOriginalDXGI();
    }
}