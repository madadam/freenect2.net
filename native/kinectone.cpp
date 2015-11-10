#include <libfreenect2/libfreenect2.hpp>
#include <libfreenect2/logger.h>

using namespace libfreenect2;

extern "C" {

//------------------------------------------------------------------------------
namespace {
  class SilentLogger : public Logger {
    void log(Logger::Level, const std::string&) override {}
  };

  static SilentLogger silent_logger;
}

//------------------------------------------------------------------------------
typedef struct Freenect2 *KinectOneContext;

KinectOneContext kinectone_context_create() {
  setGlobalLogger(&silent_logger);
  return new Freenect2;
}

void kinectone_context_destroy(KinectOneContext context) {
  delete context;
}

int kinectone_context_get_device_count(KinectOneContext context) {
  return context->enumerateDevices();
}

//------------------------------------------------------------------------------
typedef void (*KinectOneFrameCallback)(unsigned char* frame_data);

namespace {
  class FrameListener : public libfreenect2::FrameListener {
  public:
    bool onNewFrame(Frame::Type type, Frame* frame) override {
      // If I understand it correctly, returning true here means we took
      // ownership of the frame, false we left it as responsiblity of freenect2.
      if (callback && type != Frame::Ir) callback(frame->data);
      return false;
    }

    KinectOneFrameCallback callback = nullptr;
  };

  struct Device {
    Freenect2Device* device;
    FrameListener color_listener;
    FrameListener depth_listener;
  };
}


typedef struct Device *KinectOneDevice;

KinectOneDevice kinectone_device_create( KinectOneContext context
                                       , int              id)
{
  // TODO: pipeline

  auto device = context->openDevice(id);
  if (!device) return nullptr;

  auto result = new Device;
  result->device = device;
  result->device->setColorFrameListener(&result->color_listener);
  result->device->setIrAndDepthFrameListener(&result->depth_listener);

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

void kinectone_device_set_color_frame_callback( KinectOneDevice        device
                                              , KinectOneFrameCallback callback)
{
  device->color_listener.callback = callback;
}

void kinectone_device_set_depth_frame_callback( KinectOneDevice        device
                                              , KinectOneFrameCallback callback)
{
  device->depth_listener.callback = callback;
}

} // extern "C"