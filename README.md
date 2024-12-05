# Msix_0xc000027b

This repo exists to help track down error 0xc000027b when attempting to launch certain app-store apps after they have been successfully installed on a Swedish PC.  
It uses a serial port because that seems to be key to the failure of our production app in non-UK regions for some (but not all) of our customers.
  
The image shows the app connected to an ESP32 over a serial port on a USB bridge.
![App connected to an EPS32](https://raw.githubusercontent.com/Keflon/Msix_0xc000027b/refs/heads/com/SampleImage.png "App connected to an EPS32")
