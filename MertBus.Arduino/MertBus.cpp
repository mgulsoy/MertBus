/*
	MertBus.h - MertBus protokolü kütüphanesi - Mert Gülsoy
	Detaylı bilgi için .h header dosyasına bakın.

*/

#include "MertBus.h"
#include <stdlib.h>

//Constants
#define MIN_CHARS_TO_WAIT 5
#define CRC_SEED 0xE6
#define BROADCAST_ID 127


 /* Functions */
MertBus::MertBus() {
 _id = 0;
} 

void MertBus::begin(int baud) {
    //calculate the time for one byte transfer as millis
    //_waitDur = 10000.0/baud;
}
 
MertBus::MertBus(Stream &port, uint8_t TransmitEnablePin, uint8_t nodeId) {
	_port = &port;
	_sendEnablePin = TransmitEnablePin;
	_id = nodeId;
	_port->setTimeout(500); //maximum timeout period

        //_port->println("MertBus init");
        pinMode(TransmitEnablePin,OUTPUT);
        digitalWrite(TransmitEnablePin,LOW); //Begin Listening on 485 line.
        
        frameHeader.start = 0; // silence 0x00
	frameHeader.target_id = 0; //receiver (me)
	frameHeader.source_id = 0; //sender
	frameHeader.payload_size = 0;
        
        //allocate 2 bytes for buffer
        Buffer = (char*)malloc(2); //init buffer
        ChecksumError  = false; //init 
        ReceiveTimeout = false;
        InvalidHeader  = false;
        
        _errFrameCount = 0;
}

boolean MertBus::checkData() {
	if (_port->available()>MIN_CHARS_TO_WAIT) {
		//Data received/receiving
		
		frameHeader.start = _port->read(); // silence 0x00
		frameHeader.target_id = _port->read(); //receiver (me)
		frameHeader.source_id = _port->read(); //sender
		frameHeader.payload_size = _port->read(); //packet size set from header.
		
		if (frameHeader.start != 0) { //Check frame header.
                	InvalidHeader = true;
                	_errFrameCount++;
                	return false; 
                }

		//receive payload
                if (frameHeader.payload_size > 0 && frameHeader.payload_size < 33 ) { //max 32byte payload
                	free(Buffer); //free buffer
                	Buffer = (char*)malloc(frameHeader.payload_size); //init buffer  
                	while (_port->available()<=frameHeader.payload_size) { 1+1; } //wait till data available               
			ReceiveCount = _port->readBytes(Buffer,frameHeader.payload_size); //Read data, blocking until 1 sec		
                } else {
                	//invalid payload
                	ChecksumError = true; //checksum error occured
                	_errFrameCount++;
                	return false;
                }	

		//Receive checksum
		frameHeader.checksum = _port->read(); //if buffer read times out then checksum is -1
		//this ends this packet.
		
		//if packet times out dont check checksum.
		if (ReceiveCount != frameHeader.payload_size){
		        ReceiveTimeout = true;
                        _errFrameCount++;
                	return false; //timeout
                }
                ReceiveTimeout = false;

		//check if id matches:
		if ( frameHeader.target_id == _id || frameHeader.target_id == BROADCAST_ID ) {
                        //checksum calculation
                        byte crc = CRC_SEED; //seed
                        for (byte i = 0; i < ReceiveCount; i++)
                            crc ^= Buffer[i];
                        //{TODO: add ack/nack support}
                        if (int(crc) == frameHeader.checksum){
                          ChecksumError = false;
                          return true;
                        } else {
                          _errFrameCount++;
                          ChecksumError = true; //checksum error occured
                        }
                }
	}
	return false; //data discarded but it is still reachable from Buffer
}

void MertBus::sendData(char * buffer, uint8_t to_addr, uint8_t buffer_size){
    //calculate duration
    digitalWrite(_sendEnablePin,HIGH);
    float fduration = (_waitDur * buffer_size) + 0.99;
    int duration = int(fduration);
    //calculate crc
    byte crc = CRC_SEED; //seed
    for (byte i = 0; i < buffer_size; i++)
        crc ^= buffer[i];
    //begin protocol send
    _port->write((byte)0x00); //silence
    _port->write(to_addr); //target id
    _port->write(_id); //sender id
    _port->write(buffer_size); //data size       
    _port->write(buffer,buffer_size); //payload
    _port->write(crc);//checksum    
    //end protocol send
    delay(duration);
    digitalWrite(_sendEnablePin,LOW);
}

void MertBus::reply(char * buffer,uint8_t buffer_size) {
  sendData(buffer,frameHeader.source_id,buffer_size);
}

void MertBus::reply(char data) {
  char _buf[1] = { data };
  sendData(_buf, frameHeader.source_id,1);
}

uint16_t MertBus::GetFailedFrameCount(){
  return _errFrameCount;
}

