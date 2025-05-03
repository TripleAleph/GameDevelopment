#import "UnityAppController.h"
#include "Unity/IUnityGraphics.h"

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraSmokeAndFire_UnityPluginLoad(IUnityInterfaces* unityInterfaces);
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API ZibraSmokeAndFire_UnityPluginUnload();

@interface ZibraSmokeAndFireNativeTrampoline : NSObject
{												
}												
+(void)load;									
@end											
@implementation ZibraSmokeAndFireNativeTrampoline
+(void)load										
{											
	extern void (*ZibraEffects_SmokeAndFirePluginLoad)(IUnityInterfaces *);
	extern void (*ZibraEffects_SmokeAndFirePluginUnload)();
	ZibraEffects_SmokeAndFirePluginLoad = &ZibraSmokeAndFire_UnityPluginLoad;
	ZibraEffects_SmokeAndFirePluginUnload = &ZibraSmokeAndFire_UnityPluginUnload;
}												
@end
