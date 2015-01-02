/*
  MertBus Library test application.

*/

#include "MertBus.h"

#define TXEN 13
#define SELF_ID 1

MertBus mb;

void setup() {
   Serial.begin(57600);
   Serial.println("Begin init");
   mb = MertBus(Serial,TXEN,SELF_ID);
}

void loop() {
  if(mb.checkData()) {
    mb.reply("Gelen Data (SendData): ",23);
    mb.reply(mb.Buffer,mb.ReceiveCount);
  } else {
    digitalWrite(13,HIGH);
    delay(400);
    digitalWrite(13,LOW);
    delay(400);
  }
}
