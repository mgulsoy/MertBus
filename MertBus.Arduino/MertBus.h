/*
	MertBus.h - MertBus protokolü kütüphanesi

	MertBus protokolü ModBus protokolüne benzer fakat uygulaması faklıdır.

	- Bu protokol seri portu sürekli takip etmez. Seri porta veri geldiğinde IncomingData() fonksiyonu çağrılır.
		-rev: Port takip edilir. Her döngüde checkdata cağrılır.
	- Protokol portu sürekli dinleme şeklinde yapılandırır. Bunu yaparken aslında porta müdahale etmez!

*/


#ifndef MertBus_h
#define MertBus_h

#include "Arduino.h"

class MertBus  
{
  public:
  
	MertBus();//empty constructor
	/*
		Initializes the MertBus transport layer.
	*/
	MertBus(Stream &port, uint8_t transmitEnablePin, uint8_t nodeId); //Constructor
	/*
		Checks data and returns true if data received
	*/
	boolean checkData();
	/*
		Starts operation with baud rate. This function does not change Serial
		port speed. Instead calculates the on time for sendData.
	*/
	void begin(int baud);
	/*
		Accepts a byte array as data buffer and sends it to destination.
		The first value of data array should be the length of array.
	*/
	void sendData(char * buffer, uint8_t to_addr, uint8_t buffer_size );
	/*
		Replies to the last packet sender.
	*/
	void reply(char * buffer,uint8_t buffer_size);
	/*
		Gives the size of the received buffer.
	*/
        byte ReceiveCount;
	/*
		Data buffer to receive frame payload.
	*/
	char * Buffer ;
  private:
	Stream *_port;
	uint8_t _sendEnablePin,_id;
	struct FrameHeader {
		char	start;
		char	target_id;
		char    source_id;
		byte	payload_size;
		byte    checksum;
	} frameHeader;
	float _waitDur;
	
};



#endif

