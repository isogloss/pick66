#pragma once

#include "../Common/ProxyCommon.h"
#include <d3d11.h>

namespace Pick66::D3D11Proxy
{
    /// <summary>
    /// Proxy implementation for ID3D11Device
    /// </summary>
    class D3D11DeviceProxy : public ID3D11Device
    {
    public:
        D3D11DeviceProxy(ID3D11Device* original);
        virtual ~D3D11DeviceProxy();

        // IUnknown methods
        STDMETHOD(QueryInterface)(REFIID riid, void** ppvObject) override;
        STDMETHOD_(ULONG, AddRef)() override;
        STDMETHOD_(ULONG, Release)() override;

        // ID3D11Device methods (abbreviated - would include all methods)
        STDMETHOD(CreateBuffer)(const D3D11_BUFFER_DESC* pDesc, const D3D11_SUBRESOURCE_DATA* pInitialData, ID3D11Buffer** ppBuffer) override;
        STDMETHOD(CreateTexture2D)(const D3D11_TEXTURE2D_DESC* pDesc, const D3D11_SUBRESOURCE_DATA* pInitialData, ID3D11Texture2D** ppTexture2D) override;
        STDMETHOD(CreateRenderTargetView)(ID3D11Resource* pResource, const D3D11_RENDER_TARGET_VIEW_DESC* pDesc, ID3D11RenderTargetView** ppRTView) override;
        STDMETHOD_(void, GetImmediateContext)(ID3D11DeviceContext** ppImmediateContext) override;
        // ... (many more methods would be implemented)

    private:
        ID3D11Device* m_original;
        ULONG m_refCount;
    };

    /// <summary>
    /// Module functions for the D3D11 proxy DLL
    /// </summary>
    extern "C"
    {
        // D3D11 exported functions
        __declspec(dllexport) HRESULT WINAPI D3D11CreateDevice(
            IDXGIAdapter* pAdapter,
            D3D_DRIVER_TYPE DriverType,
            HMODULE Software,
            UINT Flags,
            const D3D_FEATURE_LEVEL* pFeatureLevels,
            UINT FeatureLevels,
            UINT SDKVersion,
            ID3D11Device** ppDevice,
            D3D_FEATURE_LEVEL* pFeatureLevel,
            ID3D11DeviceContext** ppImmediateContext);

        __declspec(dllexport) HRESULT WINAPI D3D11CreateDeviceAndSwapChain(
            IDXGIAdapter* pAdapter,
            D3D_DRIVER_TYPE DriverType,
            HMODULE Software,
            UINT Flags,
            const D3D_FEATURE_LEVEL* pFeatureLevels,
            UINT FeatureLevels,
            UINT SDKVersion,
            const DXGI_SWAP_CHAIN_DESC* pSwapChainDesc,
            IDXGISwapChain** ppSwapChain,
            ID3D11Device** ppDevice,
            D3D_FEATURE_LEVEL* pFeatureLevel,
            ID3D11DeviceContext** ppImmediateContext);
    }

    /// <summary>
    /// DLL management functions
    /// </summary>
    namespace DllManagement
    {
        bool Initialize();
        void Shutdown();
        HMODULE GetOriginalD3D11();
    }
}