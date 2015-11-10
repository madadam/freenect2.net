// C wrapper for libfreenect2.

#include <libfreenect2/libfreenect2.hpp>
#include <libfreenect2/logger.h>

extern "C" {

//------------------------------------------------------------------------------
namespace {
  class SilentLogger : public libfreenect2::Logger {
    void log(Logger::Level, const std::string&) override {}
  };

  static SilentLogger silent_logger;
}

//------------------------------------------------------------------------------
typedef struct libfreenect2::Freenect2 *Freenect2Context;

Freenect2Context freenect2_context_create() {
  setGlobalLogger(&silent_logger);
  return new libfreenect2::Freenect2;
}

void freenect2_context_destroy(Freenect2Context context) {
  delete context;
}

int freenect2_context_get_device_count(Freenect2Context context) {
  return context->enumerateDevices();
}

//------------------------------------------------------------------------------
typedef void (*Freenect2FrameCallback)(unsigned char* frame_data);

namespace {
  class FrameListener : public libfreenect2::FrameListener {
  public:
    bool onNewFrame( libfreenect2::Frame::Type type
                   , libfreenect2::Frame*      frame) override
    {
      // If I understand it correctly, returning true here means we took
      // ownership of the frame, false we left it as responsiblity of freenect2.
      if (callback && type != libfreenect2::Frame::Ir) callback(frame->data);
      return false;
    }

    Freenect2FrameCallback callback = nullptr;
  };

  struct Device {
    libfreenect2::Freenect2Device* device;
    FrameListener color_listener;
    FrameListener depth_listener;
  };
}


typedef struct Device *Freenect2Device;

Freenect2Device freenect2_device_create( Freenect2Context context
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

void freenect2_device_destroy(Freenect2Device device) {
  delete device->device;
  delete device;
}

void freenect2_device_start(Freenect2Device device) {
  device->device->start();
}

void freenect2_device_stop(Freenect2Device device) {
  device->device->stop();
}

void freenect2_device_set_color_frame_callback( Freenect2Device        device
                                              , Freenect2FrameCallback callback)
{
  device->color_listener.callback = callback;
}

void freenect2_device_set_depth_frame_callback( Freenect2Device        device
                                              , Freenect2FrameCallback callback)
{
  device->depth_listener.callback = callback;
}

} // extern "C"