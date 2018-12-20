#ifndef DD_CLR_PROFILER_COR_PROFILER_H_
#define DD_CLR_PROFILER_COR_PROFILER_H_

#include <atomic>
#include <mutex>
#include <string>
#include <unordered_map>
#include "cor.h"
#include "corprof.h"

#include "cor_profiler_base.h"
#include "integration.h"
#include "module_metadata.h"

namespace trace {

const DWORD kEventMask =
    COR_PRF_MONITOR_JIT_COMPILATION |
    COR_PRF_DISABLE_TRANSPARENCY_CHECKS_UNDER_FULL_TRUST | /* helps the case
                                                                where this
                                                                profiler is
                                                                used on Full
                                                                CLR */
    // COR_PRF_DISABLE_INLINING |
    COR_PRF_MONITOR_MODULE_LOADS |
    // COR_PRF_MONITOR_ASSEMBLY_LOADS |
    // COR_PRF_MONITOR_APPDOMAIN_LOADS |
    // COR_PRF_ENABLE_REJIT |
    COR_PRF_DISABLE_ALL_NGEN_IMAGES;

class CorProfiler : public CorProfilerBase {
 private:
  bool is_attached_ = false;
  std::vector<Integration> integrations_;

  std::mutex module_id_to_info_map_lock_;
  std::unordered_map<ModuleID, ModuleMetadata*> module_id_to_info_map_;

 public:
  CorProfiler();

  bool IsAttached() const;

  HRESULT STDMETHODCALLTYPE
  Initialize(IUnknown* cor_profiler_info_unknown) override;
  HRESULT STDMETHODCALLTYPE ModuleLoadFinished(ModuleID module_id,
                                               HRESULT hrStatus) override;
  HRESULT STDMETHODCALLTYPE ModuleUnloadFinished(ModuleID module_id,
                                                 HRESULT hrStatus) override;
  HRESULT STDMETHODCALLTYPE
  JITCompilationStarted(FunctionID function_id, BOOL is_safe_to_block) override;
};

// Note: Generally you should not have a single, global callback implementation,
// as that prevents your profiler from analyzing multiply loaded in-process
// side-by-side CLRs. However, this profiler implements the "profile-first"
// alternative of dealing with multiple in-process side-by-side CLR instances.
// First CLR to try to load us into this process wins; so there can only be one
// callback implementation created. (See ProfilerCallback::CreateObject.)
extern CorProfiler* profiler;  // global reference to callback object

}  // namespace trace

#endif  // DD_CLR_PROFILER_COR_PROFILER_H_
