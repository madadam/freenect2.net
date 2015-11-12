// C wrapper for libfreenect2.

#include <memory>
#include <mutex>
#include <cstring>

#include <iostream> // DEBUG
#include <thread>

#include <libfreenect2/libfreenect2.hpp>
#include <libfreenect2/logger.h>
#include <libfreenect2/registration.h>

extern "C" {

//------------------------------------------------------------------------------
namespace {
  class SilentLogger : public libfreenect2::Logger {
    void log(Logger::Level, const std::string&) override {}
  };

  static SilentLogger silent_logger;

  void debug(const char* stuff) {
    std::cout << std::this_thread::get_id() << ": " << stuff << std::endl;
  }
}

//------------------------------------------------------------------------------
typedef struct libfreenect2::Freenect2 *Freenect2Context;

Freenect2Context freenect2_context_create() {
  // setGlobalLogger(&silent_logger);
  return new libfreenect2::Freenect2;
}

void freenect2_context_destroy(Freenect2Context context) {
  delete context;
}

int freenect2_context_get_device_count(Freenect2Context context) {
  return context->enumerateDevices();
}

//------------------------------------------------------------------------------
typedef void (*Freenect2FrameCallback)(unsigned char* color, unsigned char* depth);

namespace {
  static const int COLOR_WIDTH  = 1920;
  static const int COLOR_HEIGHT = 1080;
  static const int DEPTH_WIDTH  = 512;
  static const int DEPTH_HEIGHT = 424;

  class FrameListener : public libfreenect2::FrameListener {
  public:
    FrameListener()
      : undistorted_frame(DEPTH_WIDTH, DEPTH_HEIGHT, 4)
      , registered_frame (DEPTH_WIDTH, DEPTH_HEIGHT, 4)
      , big_depth_frame  (COLOR_WIDTH, COLOR_HEIGHT + 2, 4)
    {}

    ~FrameListener() {
      // SUPER EVIL HACK: This will leak memory, but without it this
      // class crashes on destruction. No fucking idea why :(
      // registered_frame.release();
    }

    bool onNewFrame( libfreenect2::Frame::Type type
                   , libfreenect2::Frame*      frame) override
    {
      // If I understand it correctly, returning true here means we took
      // ownership of the frame, false we left it as responsiblity of freenect2.

      std::lock_guard<std::mutex> lock(mutex);

      switch (type) {
        case libfreenect2::Frame::Color:
          color_frame.reset(frame);
          break;
        case libfreenect2::Frame::Depth:
          depth_frame.reset(frame);
          break;
        default:
          return false;
      }

      if (color_frame && depth_frame) {
        registration->apply( color_frame.get()
                           , depth_frame.get()
                           , &undistorted_frame
                           , &registered_frame
                           , true
                           , &big_depth_frame);

        if (callback) {
          // Need to offset the depth frame pointer, because it contains
          // one additional row at the top and the bottom.
          callback(
              color_frame->data
            , big_depth_frame.data
              + (big_depth_frame.width * big_depth_frame.bytes_per_pixel)
          );
        }

        color_frame = nullptr;
        depth_frame = nullptr;
      }

      return true;
    }

  private:

    void copy_to_buffer( const libfreenect2::Frame& src
                       , unsigned char*             dst
                       , bool                       crop = false)
    {
      auto offset = crop ? src.width : 0;

      std::memcpy( dst
                 , src.data + offset
                 , (src.width * src.height - 2 * offset) * src.bytes_per_pixel);
    }

    std::mutex                                  mutex;

    std::unique_ptr<libfreenect2::Frame>        color_frame;
    std::unique_ptr<libfreenect2::Frame>        depth_frame;

    libfreenect2::Frame                         undistorted_frame;
    libfreenect2::Frame                         registered_frame;
    libfreenect2::Frame                         big_depth_frame;

    std::unique_ptr<libfreenect2::Registration> registration;

    Freenect2FrameCallback                      callback = nullptr;
    friend struct Device;
  };

  struct Device {
    std::unique_ptr<libfreenect2::Freenect2Device> device;
    FrameListener                                  listener;

    Device(libfreenect2::Freenect2Device* device)
      : device(device)
    {
      device->setColorFrameListener(&listener);
      device->setIrAndDepthFrameListener(&listener);
    }

    ~Device() {
      device = nullptr;
    }

    void start() {
      device->start();
      listener.registration = std::make_unique<libfreenect2::Registration>(
        device->getIrCameraParams(), device->getColorCameraParams()
      );
    }

    void stop() {
      device->stop();
    }

    void set_frame_callback(Freenect2FrameCallback callback) {
      std::lock_guard<std::mutex> lock(listener.mutex);
      listener.callback = callback;
    }
  };
}


typedef struct Device *Freenect2Device;

Freenect2Device freenect2_device_create( Freenect2Context context
                                       , int              id)
{
  auto device = context->openDevice(id);
  return device ? new Device(device) : nullptr;
}

void freenect2_device_destroy(Freenect2Device device) {
  delete device;
}

void freenect2_device_start(Freenect2Device device) {
  device->start();
}

void freenect2_device_stop(Freenect2Device device) {
  device->stop();
}

void freenect2_device_set_frame_callback( Freenect2Device        device
                                        , Freenect2FrameCallback callback)
{
  device->set_frame_callback(callback);
}

void freenect2_memory_copy( unsigned char* src
                          , unsigned char* dst
                          , std::size_t size)
{
  std::memcpy(dst, src, size);
}

} // extern "C"