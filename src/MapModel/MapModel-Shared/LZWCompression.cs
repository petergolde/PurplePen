/* Copyright (c) 2006-2008, Peter Golde
 * All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without 
 * modification, are permitted provided that the following conditions are 
 * met:
 * 
 * 1. Redistributions of source code must retain the above copyright
 * notice, this list of conditions and the following disclaimer.
 * 
 * 2. Redistributions in binary form must reproduce the above copyright
 * notice, this list of conditions and the following disclaimer in the
 * documentation and/or other materials provided with the distribution.
 * 
 * 3. Neither the name of Peter Golde, nor "Purple Pen", nor the names
 * of its contributors may be used to endorse or promote products
 * derived from this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
 * MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY
 * OF SUCH DAMAGE.
 */

using System;
using System.IO;

namespace PurplePen.MapModel
{
	class LZWCompression {
		class CompressionOutOfSpaceException: Exception {
			public CompressionOutOfSpaceException() : base("No space left in buffer for compressed data") {
			}
		}

		private const int BITS = 9;
		private const int TABLE_SIZE = 539;
		private const int HASHING_SHIFT = BITS-8;
		private const int MAX_CODE = (1 << BITS) -1;
		private const int BIT_MASK = (1 << BITS) -1;
		private const int ENDOFFILE_CODE = 257;

        private readonly int[] prefixCode;
        private readonly int[] codeValue;
		private readonly byte[] appendCharacter;
		private readonly byte[] decodeStack;
		private int nextCode = 0;
		private int stringCode = 0;
		private int bitCount = 0;
		private ulong bitBuffer = 0L;
		private byte[] buffer;
		private int bufferIndex;

		public LZWCompression() {
			this.prefixCode = new int[TABLE_SIZE];
			this.codeValue = new int[TABLE_SIZE];
			this.appendCharacter = new byte[TABLE_SIZE];
			this.decodeStack = new byte[4000];
		}

		private int findMatch(int hashPrefix, byte hashCharacter) {
			int index;
			int offset;

			// calculate the hash value
			index = (hashCharacter << HASHING_SHIFT) ^ hashPrefix;

			if (index == 0)
				offset = 1;
			else
				offset = TABLE_SIZE - index;

			// search for known pattern of for an available slot in the list
			while (true) {
				if (codeValue[index] == -1)
					break; // this position is available

				if (prefixCode[index] == hashPrefix && appendCharacter[index] == hashCharacter)
					break; // this is a match

				index -= offset;

				if (index < 0)
					index += TABLE_SIZE;
			} // while

			return(index);
		} // findMatch

		private int decodeString(int bufferPointer, int code) {
			int i = 0;

			while (code > 255) {
				decodeStack[bufferPointer++] = appendCharacter[code];
				code = prefixCode[code]; 

				// Check error in compressed bytes
				if (i++ >= 4094) {
					throw new ApplicationException("Fatal error during code expansion.");
				} // if

			} // while

			decodeStack[bufferPointer] = (byte)code;

			return bufferPointer;
		} // decodeString	

		private void putCode(int code) {
			bitBuffer |= (ulong) code << bitCount;
			bitCount += BITS;
			while (bitCount >= 8) {
				byte sss = (byte)(bitBuffer & 0xFF);
				if (bufferIndex >= buffer.Length)
					throw new CompressionOutOfSpaceException();
				buffer[bufferIndex++] = sss;
				bitBuffer >>= 8;
				bitCount -= 8;
			} // while
		} // put

		private int getCode() {
			while (bitCount < BITS) {
				if (bufferIndex >= buffer.Length)
					throw new CompressionOutOfSpaceException();
				byte sss = buffer[bufferIndex++];
				bitBuffer |= ((ulong)sss << bitCount);
				bitCount += 8;
			}

			int code = (int)(bitBuffer & BIT_MASK);
			bitBuffer >>= BITS;
			bitCount -= BITS;
			return code;
		}

		public void Compress(byte[] bufferInput, byte[] bufferOutput) {
			int startPosition = 0;
			int count = bufferInput.Length;
			int index;
			byte character;

			this.bitBuffer = 0;
			this.bitCount = 0;

			this.nextCode = 258;				// Next code is the next available string code
			for (int i=0;i<TABLE_SIZE;i++)		// Clear out the string table before starting
				this.codeValue[i]=-1;

			// We need to initialize stringCode before starting the algorithm
			stringCode = bufferInput[0];
			startPosition++;

			buffer = bufferOutput;
			bufferIndex = 0;

			// Process the bytes in the buffer
			for (int i=startPosition;i<count;i++) {
				character = bufferInput[i];
				index = findMatch(stringCode,character);		// See if the string is in
				if (codeValue[index] != -1)						// the table.  If it is,   
					stringCode = codeValue[index];				// get the code value.  If 
				else {											// the string is not in the
				 												// table, try to add it.   
					if (nextCode <= MAX_CODE) {
						codeValue[index] = nextCode++;
						prefixCode[index] = stringCode;
						appendCharacter[index] = character;
					}

					putCode(stringCode);
					stringCode = character;            
				}  // else                                 

			} // for

			putCode(stringCode);		// this is still not written to the stream
			putCode(ENDOFFILE_CODE);			// mark the end of the stream

			// make sure that the whole buffer is written to the stream
			if (bitCount > 0) {
				if (bufferIndex >= buffer.Length)
					throw new CompressionOutOfSpaceException();
				buffer[bufferIndex++] = (byte)(bitBuffer & 0xFF);
			}
		}

		public void Expand(byte[] inputBuffer, byte[] outputBuffer) {
			this.buffer = inputBuffer;
			this.bufferIndex = 0;
			this.bitBuffer = 0;
			this.bitCount = 0;

			int outputIndex = 0;

			this.nextCode = 258;				// Next code is the next available string code
			for (int i=0;i<TABLE_SIZE;i++)		// Clear out the string table before starting
				this.codeValue[i]=-1;

			byte character;
			int newCode;
			int decodeStackPointer = 0;
			int stringPointer = -1;
			int oldCode;
				
			oldCode = getCode();
			character=(byte)oldCode;    
			if (outputIndex >= outputBuffer.Length)
				throw new CompressionOutOfSpaceException();
			outputBuffer[outputIndex++] = character;

			while ((newCode = getCode()) != ENDOFFILE_CODE) {
				if (newCode >= nextCode) {
					decodeStack[decodeStackPointer] = character;
					stringPointer = decodeString(decodeStackPointer+1,oldCode);
				} // if
				else 	// May be there is a matching pattern
					stringPointer = decodeString(decodeStackPointer,newCode);

				character = decodeStack[stringPointer];
				// Process all available bytes from the stack
				while (stringPointer >= decodeStackPointer) {
					if (outputIndex >= outputBuffer.Length)
						throw new CompressionOutOfSpaceException();
					outputBuffer[outputIndex++] = decodeStack[stringPointer--];
				} // while

				if (nextCode <= MAX_CODE) {
					prefixCode[nextCode] = oldCode;
					appendCharacter[nextCode] = character;
					nextCode++;
				}
				oldCode = newCode;

			} // while

		}
	}

}
