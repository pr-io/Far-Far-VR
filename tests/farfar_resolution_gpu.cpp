#include "../Engine/FarFarWestResolution.hpp"
#include <windows.h>
#include <d3d12.h>
#include <dxgi1_6.h>
#include <wrl.h>
#include <cassert>
#include <iostream>
#include <vector>
using Microsoft::WRL::ComPtr;
int main(){
 ComPtr<ID3D12Debug> debug;assert(SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debug))));debug->EnableDebugLayer();
 ComPtr<IDXGIFactory4> factory;assert(SUCCEEDED(CreateDXGIFactory1(IID_PPV_ARGS(&factory))));
 ComPtr<IDXGIAdapter> warp;assert(SUCCEEDED(factory->EnumWarpAdapter(IID_PPV_ARGS(&warp))));
 ComPtr<ID3D12Device> device;assert(SUCCEEDED(D3D12CreateDevice(warp.Get(),D3D_FEATURE_LEVEL_11_0,IID_PPV_ARGS(&device))));
 auto texture=[&](UINT width,UINT height,D3D12_RESOURCE_STATES state){
  D3D12_HEAP_PROPERTIES heap{};heap.Type=D3D12_HEAP_TYPE_DEFAULT;
  D3D12_RESOURCE_DESC desc{};desc.Dimension=D3D12_RESOURCE_DIMENSION_TEXTURE2D;desc.Width=width;desc.Height=height;
  desc.DepthOrArraySize=1;desc.MipLevels=1;desc.Format=DXGI_FORMAT_B8G8R8A8_UNORM;desc.SampleDesc.Count=1;
  ComPtr<ID3D12Resource> resource;assert(SUCCEEDED(device->CreateCommittedResource(&heap,D3D12_HEAP_FLAG_NONE,&desc,state,nullptr,IID_PPV_ARGS(&resource))));return resource;
 };
 ComPtr<ID3D12InfoQueue> info;assert(SUCCEEDED(device.As(&info)));
 auto errors=[&](){unsigned count=0;for(UINT64 i=0;i<info->GetNumStoredMessages();++i){SIZE_T bytes{};info->GetMessage(i,nullptr,&bytes);std::vector<char> buffer(bytes);auto* message=reinterpret_cast<D3D12_MESSAGE*>(buffer.data());info->GetMessage(i,message,&bytes);if(message->Severity<=D3D12_MESSAGE_SEVERITY_ERROR){++count;std::cout<<message->pDescription<<"\n";}}info->ClearStoredMessages();return count;};
 for(const bool packed:{false,true})for(const auto width:{64u,96u}){
  const UINT height=48,old_width=width==64?96:64;
  auto left=texture(packed?width*2:width,height,D3D12_RESOURCE_STATE_COPY_SOURCE),right=texture(width,height,D3D12_RESOURCE_STATE_COPY_SOURCE);
  auto output=texture(width*2,height,D3D12_RESOURCE_STATE_COPY_DEST);
  auto record=[&](UINT cached_width){
   ComPtr<ID3D12CommandAllocator> allocator;assert(SUCCEEDED(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT,IID_PPV_ARGS(&allocator))));
   ComPtr<ID3D12GraphicsCommandList> list;assert(SUCCEEDED(device->CreateCommandList(0,D3D12_COMMAND_LIST_TYPE_DIRECT,allocator.Get(),nullptr,IID_PPV_ARGS(&list))));
   if(farfarvr::resize_step(width,height,left->GetDesc().Width,height,cached_width*2,height,true)==farfarvr::ResizeStep::ready){
    D3D12_TEXTURE_COPY_LOCATION dst{};dst.pResource=output.Get();dst.Type=D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    D3D12_TEXTURE_COPY_LOCATION src{};src.pResource=left.Get();src.Type=D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    D3D12_BOX box{0,0,0,cached_width,height,1};list->CopyTextureRegion(&dst,0,0,0,&src,&box);
    src.pResource=right.Get();box.right=width;list->CopyTextureRegion(&dst,cached_width,0,0,&src,&box);
   }
   const auto result=list->Close();if(FAILED(result))return result;
   D3D12_COMMAND_QUEUE_DESC qdesc{};qdesc.Type=D3D12_COMMAND_LIST_TYPE_DIRECT;
   ComPtr<ID3D12CommandQueue> queue;assert(SUCCEEDED(device->CreateCommandQueue(&qdesc,IID_PPV_ARGS(&queue))));
   ComPtr<ID3D12Fence> fence;assert(SUCCEEDED(device->CreateFence(0,D3D12_FENCE_FLAG_NONE,IID_PPV_ARGS(&fence))));
   ID3D12CommandList* lists[]{list.Get()};queue->ExecuteCommandLists(1,lists);assert(SUCCEEDED(queue->Signal(fence.Get(),1)));
   auto event=CreateEventW(nullptr,FALSE,FALSE,nullptr);assert(event);assert(SUCCEEDED(fence->SetEventOnCompletion(1,event)));
   assert(WaitForSingleObject(event,5000)==WAIT_OBJECT_0);CloseHandle(event);return result;
  };
  assert(SUCCEEDED(record(old_width)));assert(errors()==0); // Transitional frame records no invalid copy.
  assert(SUCCEEDED(record(width)));assert(errors()==0); // Both new eyes copy successfully once rebuilt.
 }
 std::cout<<"D3D12 WARP executed guarded upscale/downscale transitions and resized stereo copies without validation errors.\n";
}
