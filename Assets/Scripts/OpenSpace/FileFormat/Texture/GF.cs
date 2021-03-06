﻿// Adapted from Rayman2Lib by szymski
// https://github.com/szymski/Rayman2Lib/blob/master/d_tools/rayman2lib/source/formats/gf.d

using System;
using System.IO;
using UnityEngine;
/*
GF Header:
4 bytes - signature
4 bytes - width
4 bytes - height
1 byte  - channel count
1 byte  - repeat byte

Now, we need to read the channels

Channel:
For each pixel (width*height):
1 byte - color value

If color value 1 equals repeat byte from header, we read more values:
1 byte - color value
1 byte - repeat count

Otherwise:
Channel pixel = color value

*/

namespace OpenSpace.FileFormat.Texture {
    public class GF {
        public uint width, height;
        public byte channels;
        public byte repeatByte;
        public uint format;
        public uint channelPixels;
        public byte byte1;
        public byte byte2;
        public byte byte3;
        public uint num4;
        public bool isTransparent = false;
        public bool isLittleEndian = true;
        public byte montrealType;
        public ushort paletteNumColors;
        public byte paletteBytesPerColor;
        public byte[] palette = null;
        public Color[] pixels;

        public GF(byte[] bytes) : this(new MemoryStream(bytes)) {}
        /*public GF(byte[] bytes) {
            Util.ByteArrayToFile(MapLoader.Loader.gameDataBinFolder + "hi" + bytes.Length + ".lol", bytes);
            GF gf = new GF(new MemoryStream(bytes));
            pixels = gf.pixels;
            throw new Exception("exported");
        }*/

        public GF(string filePath) : this(FileSystem.GetFileReadStream(filePath)) { }

        public GF(Stream stream) {
            MapLoader l = MapLoader.Loader;
            Reader r = new Reader(stream, isLittleEndian);
            if (Settings.s.engineVersion == Settings.EngineVersion.Montreal) {
                byte version = r.ReadByte();
                format = 1555;
            } else {
                format = 8888;
                if (Settings.s.platform != Settings.Platform.iOS && Settings.s.game != Settings.Game.TTSE) format = r.ReadUInt32();
            }

            width = r.ReadUInt32();
            height = r.ReadUInt32();
            channelPixels = width * height;

            channels = r.ReadByte();
            byte enlargeByte = 0;
            if (Settings.s.engineVersion == Settings.EngineVersion.R3 && Settings.s.game != Settings.Game.Dinosaur && Settings.s.game != Settings.Game.LargoWinch) enlargeByte = r.ReadByte();
            uint w = width, h = height;
			if (enlargeByte > 0) {
				channelPixels = 0;
				for (int i = 0; i < enlargeByte; i++) {
					channelPixels += (w * h);
					w /= 2;
					h /= 2;
				}
			}
            repeatByte = r.ReadByte();
            if (Settings.s.engineVersion == Settings.EngineVersion.Montreal) {
                paletteNumColors = r.ReadUInt16();
                paletteBytesPerColor = r.ReadByte();
                byte1 = r.ReadByte();
                byte2 = r.ReadByte();
                byte3 = r.ReadByte();
                num4 = r.ReadUInt32();
                channelPixels = r.ReadUInt32(); // Hype has mipmaps
                montrealType = r.ReadByte();
                if (paletteNumColors != 0 && paletteBytesPerColor != 0) {
                    palette = r.ReadBytes(paletteBytesPerColor * paletteNumColors);
                }
                switch (montrealType) {
                    case 5: format = 0; break; // palette
                    case 10: format = 565; break; // unsure
                    case 11: format = 1555; break;
                    case 12: format = 4444; break; // unsure
                    default: throw new Exception("unknown Montreal GF format " + montrealType + "!");
                }
            }

            pixels = new Color[width * height];
            byte[] blue_channel = null, green_channel = null, red_channel = null, alpha_channel = null;

            if (channels >= 3) {
                blue_channel = ReadChannel(r, repeatByte, channelPixels);
                green_channel = ReadChannel(r, repeatByte, channelPixels);
                red_channel = ReadChannel(r, repeatByte, channelPixels);
                if (channels == 4) {
                    alpha_channel = ReadChannel(r, repeatByte, channelPixels);
                    isTransparent = true;
                }
            } else if (channels == 2) {
                byte[] channel_1 = ReadChannel(r, repeatByte, channelPixels);
                byte[] channel_2 = ReadChannel(r, repeatByte, channelPixels);

                red_channel = new byte[channelPixels];
                green_channel = new byte[channelPixels];
                blue_channel = new byte[channelPixels];
                alpha_channel = new byte[channelPixels];
                if (format == 1555 || format == 4444) isTransparent = true;
                for (int i = 0; i < channelPixels; i++) {
                    ushort pixel = BitConverter.ToUInt16(new byte[] { channel_1[i], channel_2[i] }, 0); // RRRRR, GGGGGG, BBBBB (565)
                    uint red, green, blue, alpha;
                    switch (format) {
                        case 88:
                            alpha_channel[i] = channel_2[i];
                            red_channel[i] = channel_1[i];
                            blue_channel[i] = channel_1[i];
                            green_channel[i] = channel_1[i];
                            break;
                        case 4444:
                            alpha = extractBits(pixel, 4, 12);
                            red = extractBits(pixel, 4, 8);
                            green = extractBits(pixel, 4, 4);
                            blue = extractBits(pixel, 4, 0);
                            red_channel[i] = (byte)((red / 15.0f) * 255.0f);
                            green_channel[i] = (byte)((green / 15.0f) * 255.0f);
                            blue_channel[i] = (byte)((blue / 15.0f) * 255.0f);
                            alpha_channel[i] = (byte)((alpha / 15.0f) * 255.0f);
                            break;
                        case 1555:
							/*
                            alpha = extractBits(pixel, 1, 15);
                            red = extractBits(pixel, 5, 10);
                            green = extractBits(pixel, 5, 5);
                            blue = extractBits(pixel, 5, 0);
							*/
							alpha = extractBits(pixel, 1, 15);
							red = extractBits(pixel, 5, 10);
							green = extractBits(pixel, 5, 5);
							blue = extractBits(pixel, 5, 0);

							red_channel[i] = (byte)((red / 31.0f) * 255.0f);
                            green_channel[i] = (byte)((green / 31.0f) * 255.0f);
                            blue_channel[i] = (byte)((blue / 31.0f) * 255.0f);
                            alpha_channel[i] = (byte)(alpha * 255);
                            break;
                        case 565:
                        default: // 565
							red = extractBits(pixel, 5, 11);
							green = extractBits(pixel, 6, 5);
							blue = extractBits(pixel, 5, 0);

                            red_channel[i] = (byte)((red / 31.0f) * 255.0f);
                            green_channel[i] = (byte)((green / 63.0f) * 255.0f);
                            blue_channel[i] = (byte)((blue / 31.0f) * 255.0f);
                            break;
                    }
                }
            } else if (channels == 1) {
                byte[] channel_1 = ReadChannel(r, repeatByte, channelPixels);
                red_channel = new byte[channelPixels];
                green_channel = new byte[channelPixels];
                blue_channel = new byte[channelPixels];
                for (int i = 0; i < channelPixels; i++) {
                    if (palette != null) {
                        red_channel[i] = palette[channel_1[i] * paletteBytesPerColor + 2];
                        green_channel[i] = palette[channel_1[i] * paletteBytesPerColor + 1];
                        blue_channel[i] = palette[channel_1[i] * paletteBytesPerColor + 0];
                    } else {
                        red_channel[i] = channel_1[i];
                        blue_channel[i] = channel_1[i];
                        green_channel[i] = channel_1[i];
                    }
                }
            }
            for (int i = 0; i < width * height; i++) {
                if (isTransparent) {
                    pixels[i] = new Color(red_channel[i] / 255f, green_channel[i] / 255f, blue_channel[i] / 255f, alpha_channel[i] / 255f);
                } else {
                    float alphaValue = 1f;
                    //if (red_channel[i] == 0 && green_channel[i] == 0 && blue_channel[i] == 0) alphaValue = 0f;
                    pixels[i] = new Color(red_channel[i] / 255f, green_channel[i] / 255f, blue_channel[i] / 255f, alphaValue);
                }
            }
            /*for (int y = 0; y < height / 2; y++) {
                for (int x = 0; x < width / 2; x++) {
                    Color temp = pixels[y * width + x];
                    pixels[y * width + x] = pixels[(height - 1 - y) * width + x];
                    pixels[(height - 1 - y) * width + x] = temp;
                }
            }*/
            r.Close();
        }

        byte[] ReadChannel(Reader r, byte repeatByte, uint pixels) {
            byte[] channel = new byte[pixels];

            int pixel = 0;

            while (pixel < pixels) {
                byte b1 = r.ReadByte();
                if (b1 == repeatByte) {
                    byte value = r.ReadByte();
                    byte count = r.ReadByte();

                    for (int i = 0; i < count; ++i) {
                        if (pixel < pixels) channel[pixel] = value;
                        pixel++;
                    }
                } else {
                    channel[pixel] = b1;
                    pixel++;
                }
            }

            return channel;
        }

        public Texture2D GetTexture() {
            Texture2D tex = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        static uint extractBits(int number, int count, int offset) {
            return (uint)(((1 << count) - 1) & (number >> (offset)));
        }
    }
}