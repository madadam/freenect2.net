.PHONY: clean

LIBRARY := libkinectone.so
CXX := gcc

$(LIBRARY): kinectone.cpp kinectone.h
	@$(CXX) -std=c++14 -fPIC -shared -o $@ $< -lfreenect2

clean:
	@rm -f $(LIBRARY)
