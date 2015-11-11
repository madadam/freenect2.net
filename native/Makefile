.PHONY: clean

LIBRARY := libfreenect2c.so
CXX := gcc

CFLAGS := -std=c++14 -fPIC -shared
LFLAGS := -L.. -Wl,-rpath,\$$ORIGIN -lfreenect2

ifeq ($(BUILD_TYPE),release)
CFLAGS := -O2 $(CFLAGS) -DNDEBUG
else
CFLAGS := -ggdb $(CFLAGS)
endif

$(LIBRARY): freenect2c.cpp Makefile
	@$(CXX) $(CFLAGS) -o $@ $< $(LFLAGS)

clean:
	@rm -f $(LIBRARY)
