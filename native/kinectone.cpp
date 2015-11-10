#include <libfreenect2/libfreenect2.hpp>

using namespace libfreenect2;

extern "C" {

//------------------------------------------------------------------------------
typedef struct Freenect2 *KinectOneContext;

KinectOneContext kinectone_context_create() {
  return new Freenect2;
}

void kinectone_context_destroy(KinectOneContext context) {
  delete context;
}

int kinectone_context_get_device_count(KinectOneContext context) {
  return context->enumerateDevices();
}

//------------------------------------------------------------------------------
struct _KinectOneDevice {
  Freenect2Device* device;
};

typedef struct _KinectOneDevice *KinectOneDevice;

KinectOneDevice kinectone_device_create( KinectOneContext context
                                       , int              id)
{
  // TODO: pipeline

  auto device = context->openDevice(id);
  if (!device) return nullptr;

  auto result = new _KinectOneDevice;
  result->device = device;

  return result;
}

void kinectone_device_destroy(KinectOneDevice device) {
  delete device->device;
  delete device;
}

void kinectone_device_start(KinectOneDevice device) {
  device->device->start();
}

void kinectone_device_stop(KinectOneDevice device) {
  device->device->stop();
}

} // extern "C"