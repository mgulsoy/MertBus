/*
  MertBus Library test application. Mert Gülsoy 2015
  
  This is a test application for using the library. Please read the comments for help.

*/

#include "MertBus.h"

#define TXEN 13   //define transmit_enable (DE) pin
#define SELF_ID 1 //define id (address) of this node.

MertBus mb;   //create instance. This allows you to use more of this if you have more serial ports like mega2560

void setup() {
   Serial.begin(57600);   //prepare serial port
   Serial.println("Begin init");  //even the MertBus instance uses the serial port you can still utilize the port
   mb = MertBus(Serial,TXEN,SELF_ID); //initialize instance with Serial, transmit enable pin and id (address)
}

void loop() {
  /* mb.checkData() is the way of checking if any valid data arrived.
     If any valid data received the this function parses it and returns true. Then
     data is placed into a buffer of char[] mb.Buffer.
     Received data byte count is a variable so it can be checked from mb.ReceiveCount
  */
  if(mb.checkData()) { 
    for (int i=0; i<mb.ReceiveCount; i++) { //reply every bytes back to sender.
      mb.reply("Gelen Data (SendData): ",mb.Buffer[i]); //with mb.reply it is not needed to know the sender id. 
    }
    mb.reply(mb.Buffer,mb.ReceiveCount);
  } else { //no data. Do sth else...
    digitalWrite(13,HIGH);
    delay(400);
    digitalWrite(13,LOW);
    delay(400);
  }
}
