.PHONY: clean

LIBRARY := libfreenect2c.so
CXX := gcc

$(LIBRARY): freenect2c.cpp
	@$(CXX) -std=c++14 -fPIC -shared -o $@ $< -lfreenect2

clean:
	@rm -f $(LIBRARY)
