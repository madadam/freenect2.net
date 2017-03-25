// C wrapper for libfreenect2.

#include <memory>
#include <mutex>
#include <cstring>

// DEBUG
#include <iostream>
#include <fstream>

#include <libfreenect2/libfreenect2.hpp>
#include <libfreenect2/logger.h>
#include <libfreenect2/packet_pipeline.h>
#include <libfreenect2/registration.h>

extern "C" {

//------------------------------------------------------------------------------
namespace {
  class SilentLogger : public libfreenect2::Logger {
    void log(Logger::Level, const std::string&) override {}
  };

  static SilentLogger silent_logger;

  // class FileLogger : public libfreenect2::Logger {
  //   std::ofstream file;

  //   public:

  //   FileLogger(const char *filename)
  //     : file(filename)
  //   {
  //     level_ = Info;
  //   }

  //   virtual void log(Level level, const std::string &message) {
  //     file << "[" << libfreenect2::Logger::level2str(level) << "] " << message << std::endl;
  //   }
  // };

  // static FileLogger file_logger("/tmp/libfreenect2c.log");
}

//------------------------------------------------------------------------------
typedef struct libfreenect2::Freenect2 *Freenect2Context;

Freenect2Context freenect2_context_create() {
  setGlobalLogger(&silent_logger);
  // setGlobalLogger(&file_logger);

  return new libfreenect2::Freenect2;
}

void freenect2_context_destroy(Freenect2Context context) {
  delete context;
}

int freenect2_context_get_device_count(Freenect2Context context) {
  return context->enumerateDevices();
}

//------------------------------------------------------------------------------
typedef void (*Freenect2FrameCallback)(unsigned char* color, unsigned char* depth, unsigned char* bigdepth);

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
              color_frame->data,
              depth_frame->data,
              big_depth_frame.data
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

enum Freenect2Pipeline : int {
  FREENECT2_PIPELINE_DEFAULT = 0,
  FREENECT2_PIPELINE_CPU     = 1,
  FREENECT2_PIPELINE_OPENGL  = 2,
  FREENECT2_PIPELINE_OPENCL  = 3,
  FREENECT2_PIPELINE_CUDA    = 4
};

Freenect2Device freenect2_device_create( Freenect2Context  context
                                       , int               id
                                       , Freenect2Pipeline pipeline_type)
{
  libfreenect2::Freenect2Device* device = nullptr;

  switch (pipeline_type) {
    case FREENECT2_PIPELINE_CPU:
      device = context->openDevice(id, new libfreenect2::CpuPacketPipeline);
      break;
    case FREENECT2_PIPELINE_OPENGL:
      device = context->openDevice(id, new libfreenect2::OpenGLPacketPipeline);
      break;
    case FREENECT2_PIPELINE_OPENCL:
      device = context->openDevice(id, new libfreenect2::OpenCLPacketPipeline);
      break;
    case FREENECT2_PIPELINE_CUDA:
      device = context->openDevice(id, new libfreenect2::CudaPacketPipeline);
      break;
    default:
      device = context->openDevice(id);
  }

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
