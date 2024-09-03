using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

internal static class CompressShell
{
	public struct State
	{
		public uint Index;

		public void Init()
		{
			Index = 0u;
		}

		public void UpdateChar()
		{
			if (Index < 4)
			{
				Index = 0u;
			}
			else if (Index < 10)
			{
				Index -= 3u;
			}
			else
			{
				Index -= 6u;
			}
		}

		public void UpdateMatch()
		{
			Index = ((Index < 7) ? 7u : 10u);
		}

		public void UpdateRep()
		{
			Index = ((Index < 7) ? 8u : 11u);
		}

		public void UpdateShortRep()
		{
			Index = ((Index < 7) ? 9u : 11u);
		}

		public bool IsCharState()
		{
			return Index < 7;
		}
	}

	public class OutWindow
	{
		private byte[] _buffer;

		private uint _pos;

		private uint _windowSize;

		private uint _streamPos;

		private Stream _stream;

		public void Create(uint windowSize)
		{
			if (_windowSize != windowSize)
			{
				_buffer = new byte[windowSize];
			}
			_windowSize = windowSize;
			_pos = 0u;
			_streamPos = 0u;
		}

		public void Init(Stream stream, bool solid)
		{
			ReleaseStream();
			_stream = stream;
			if (!solid)
			{
				_streamPos = 0u;
				_pos = 0u;
			}
		}

		public void ReleaseStream()
		{
			Flush();
			_stream = null;
			Buffer.BlockCopy(new byte[_buffer.Length], 0, _buffer, 0, _buffer.Length);
		}

		public void Flush()
		{
			uint num = _pos - _streamPos;
			if (num != 0)
			{
				_stream.Write(_buffer, (int)_streamPos, (int)num);
				if (_pos >= _windowSize)
				{
					_pos = 0u;
				}
				_streamPos = _pos;
			}
		}

		public void CopyBlock(uint distance, uint len)
		{
			uint num = _pos - distance - 1;
			if (num >= _windowSize)
			{
				num += _windowSize;
			}
			while (len != 0)
			{
				if (num >= _windowSize)
				{
					num = 0u;
				}
				_buffer[_pos++] = _buffer[num++];
				if (_pos >= _windowSize)
				{
					Flush();
				}
				len--;
			}
		}

		public void PutByte(byte b)
		{
			_buffer[_pos++] = b;
			if (_pos >= _windowSize)
			{
				Flush();
			}
		}

		public byte GetByte(uint distance)
		{
			uint num = _pos - distance - 1;
			if (num >= _windowSize)
			{
				num += _windowSize;
			}
			return _buffer[num];
		}
	}

	private class Decoder
	{
		public uint Range;

		public uint Code;

		public Stream Stream;

		public void Init(Stream stream)
		{
			Stream = stream;
			Code = 0u;
			Range = uint.MaxValue;
			for (int i = 0; i < 5; i++)
			{
				Code = (Code << 8) | (byte)Stream.ReadByte();
			}
		}

		public void ReleaseStream()
		{
			Stream = null;
		}



		public uint DecodeDirectBits(int numTotalBits)
		{
			uint num = Range;
			uint num2 = Code;
			uint num3 = 0u;
			for (int num4 = numTotalBits; num4 > 0; num4--)
			{
				num >>= 1;
				uint num5 = num2 - num >> -980632157 + (920725017 - 189029305 + 1244147344) - ((0x35E8A604 ^ 0x5BBBCDE4) - ((0x5A78CD30 ^ 0x38323FB5) - (0x5C672DB9 ^ 0x732E10A0)));
				num2 -= num & (num5 - 1);
				num3 = (num3 << 1) | (1 - num5);
				if (num < -738431128 + 755208344)
				{
					num2 = (num2 << 8) | (byte)Stream.ReadByte();
					num <<= 8;
				}
			}
			Range = num;
			Code = num2;
			return num3;
		}
	}

	private struct BitDecoder
	{
		private uint Prob;

		public void Init()
		{
			Prob = 1024u;
		}

		public uint Decode(Decoder rangeDecoder)
		{
			uint num = (rangeDecoder.Range >> 11) * Prob;
			if (rangeDecoder.Code < num)
			{
				rangeDecoder.Range = num;
				Prob += 1912689102 - 1912687054 - Prob >> 5;
				if (rangeDecoder.Range < -1559329687 + 1576106903)
				{
					rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
				}
				return 0u;
			}
			rangeDecoder.Range -= num;
			rangeDecoder.Code -= num;
			Prob -= Prob >> 5;
			if (rangeDecoder.Range < 16777216)
			{
				rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
				rangeDecoder.Range <<= 8;
			}
			return 1u;
		}
	}

	private struct BitTreeDecoder
	{
		private BitDecoder[] Models;

		private int NumBitLevels;

		public BitTreeDecoder(int numBitLevels)
		{
			NumBitLevels = numBitLevels;
			Models = new BitDecoder[1 << (numBitLevels & 0x1F)];
		}

		public void Init()
		{
			for (uint num = 1u; num < 1 << (NumBitLevels & 0x1F); num++)
			{
				Models[num].Init();
			}
		}

		public uint Decode(Decoder rangeDecoder)
		{
			uint num = 1u;
			for (int num2 = NumBitLevels; num2 > 0; num2--)
			{
				num = (num << 1) + Models[num].Decode(rangeDecoder);
			}
			return num - (uint)(1 << (NumBitLevels & 0x1F));
		}

		public uint ReverseDecode(Decoder rangeDecoder)
		{
			uint num = 1u;
			uint num2 = 0u;
			for (int i = 0; i < NumBitLevels; i++)
			{
				uint num3 = Models[num].Decode(rangeDecoder);
				num <<= 1;
				num += num3;
				num2 |= num3 << (i & 0x1F);
			}
			return num2;
		}

		public static uint ReverseDecode(BitDecoder[] Models, uint startIndex, Decoder rangeDecoder, int NumBitLevels)
		{
			uint num = 1u;
			uint num2 = 0u;
			for (int i = 0; i < NumBitLevels; i++)
			{
				uint num3 = Models[startIndex + num].Decode(rangeDecoder);
				num <<= 1;
				num += num3;
				num2 |= num3 << (i & 0x1F);
			}
			return num2;
		}
	}

	public class LzmaDecoder
	{
		private class LenDecoder
		{
			private BitDecoder m_Choice = default(BitDecoder);

			private BitDecoder m_Choice2 = default(BitDecoder);

			private BitTreeDecoder[] m_LowCoder = new BitTreeDecoder[16];

			private BitTreeDecoder[] m_MidCoder = new BitTreeDecoder[16];

			private BitTreeDecoder m_HighCoder = new BitTreeDecoder(8);

			private uint m_NumPosStates;

			public void Create(uint numPosStates)
			{
				for (uint num = m_NumPosStates; num < numPosStates; num++)
				{
					ref BitTreeDecoder reference = ref m_LowCoder[num];
					reference = new BitTreeDecoder(3);
					ref BitTreeDecoder reference2 = ref m_MidCoder[num];
					reference2 = new BitTreeDecoder(3);
				}
				m_NumPosStates = numPosStates;
			}

			public void Init()
			{
				m_Choice.Init();
				for (uint num = 0u; num < m_NumPosStates; num++)
				{
					m_LowCoder[num].Init();
					m_MidCoder[num].Init();
				}
				m_Choice2.Init();
				m_HighCoder.Init();
			}

			public uint Decode(Decoder rangeDecoder, uint posState)
			{
				if (m_Choice.Decode(rangeDecoder) == 0)
				{
					return m_LowCoder[posState].Decode(rangeDecoder);
				}
				uint num = 8u;
				if (m_Choice2.Decode(rangeDecoder) == 0)
				{
					return num + m_MidCoder[posState].Decode(rangeDecoder);
				}
				num += 8;
				return num + m_HighCoder.Decode(rangeDecoder);
			}
		}

		private class LiteralDecoder
		{
			private struct Decoder2
			{
				private BitDecoder[] m_Decoders;

				public void Create()
				{
					m_Decoders = new BitDecoder[-1793689521 + (0x145B4707 ^ 0x7EB2CDB6)];
				}

				public void Init()
				{
					for (int i = 0; i < -1535061796 + ((0x1300A776 ^ 0x444E58B9) + (-1220990081 + (0x611AA554 ^ 0x2DEDA382))); i++)
					{
						m_Decoders[i].Init();
					}
				}

				public byte DecodeNormal(Decoder rangeDecoder)
				{
                    uint num = 1U;
                    unchecked
					{
						
						do
						{
							num = (num << 1) | this.m_Decoders[(int)((UIntPtr)num)].Decode(rangeDecoder);
						}
						while (num < 4270852855U + ((128842448U + 1012521681U) ^ (346083889U ^ ((349433856U + 760905991U) ^ 8552380U)) ^ ((3592853402U - 1767354880U) ^ 388640572U) ^ (451670097U ^ (4278505598U + 2066043383U))) - (3486353874U + (1729046827U - 657351324U + ((4208284750U + ((2578458759U - 2005446033U) ^ 1917574619U)) ^ (4053018685U - (145198454U ^ (1934270942U + 176986491U))))) - (2954039789U - (2820649831U - (840789214U ^ 1421733138U) + (3235976884U + 1851788136U)))));
					}
                    return (byte)num;
                }

				public byte DecodeWithMatchByte(Decoder rangeDecoder, byte matchByte)
				{
					uint num = 1u;
					do
					{
						uint num2 = (uint)(matchByte >> 7) & 1u;
						matchByte <<= 1;
						uint num3 = m_Decoders[(1 + num2 << 8) + num].Decode(rangeDecoder);
						num = (num << 1) | num3;
						if (num2 != num3)
						{
							while (num < (0x2F016B47 ^ 0x259F5B14) + (-1312495569 + 1391323666 + 257182528) - (8317791 + 505831797))
							{
								num = (num << 1) | m_Decoders[num].Decode(rangeDecoder);
							}
							break;
						}
					}
					while (num < (0x4CD18B46 ^ 0x6F490A56) - 597196816);
					return (byte)num;
				}
			}

			private Decoder2[] m_Coders;

			private int m_NumPrevBits;

			private int m_NumPosBits;

			private uint m_PosMask;

			public void Create(int numPosBits, int numPrevBits)
			{
				if (m_Coders == null || m_NumPrevBits != numPrevBits || m_NumPosBits != numPosBits)
				{
					m_NumPosBits = numPosBits;
					m_PosMask = (uint)((1 << (numPosBits & 0x1F)) - 1);
					m_NumPrevBits = numPrevBits;
					uint num = (uint)(1 << ((m_NumPrevBits + m_NumPosBits) & 0x1F));
					m_Coders = new Decoder2[num];
					for (uint num2 = 0u; num2 < num; num2++)
					{
						m_Coders[num2].Create();
					}
				}
			}

			public void Init()
			{
				uint num = (uint)(1 << ((m_NumPrevBits + m_NumPosBits) & 0x1F));
				for (uint num2 = 0u; num2 < num; num2++)
				{
					m_Coders[num2].Init();
				}
			}

			private uint GetState(uint pos, byte prevByte)
			{
				return ((pos & m_PosMask) << (m_NumPrevBits & 0x1F)) + (uint)(prevByte >> ((8 - m_NumPrevBits) & 0x1F));
			}

			public byte DecodeNormal(Decoder rangeDecoder, uint pos, byte prevByte)
			{
				return m_Coders[GetState(pos, prevByte)].DecodeNormal(rangeDecoder);
			}

			public byte DecodeWithMatchByte(Decoder rangeDecoder, uint pos, byte prevByte, byte matchByte)
			{
				return m_Coders[GetState(pos, prevByte)].DecodeWithMatchByte(rangeDecoder, matchByte);
			}
		}

		private OutWindow m_OutWindow = new OutWindow();

		private Decoder m_RangeDecoder = new Decoder();

		private BitDecoder[] m_IsMatchDecoders = new BitDecoder[2092996358 - 2092996166];

		private BitDecoder[] m_IsRepDecoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG0Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG1Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG2Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRep0LongDecoders = new BitDecoder[0x5CC1C2AE ^ 0x5CC1C26E];

		private BitTreeDecoder[] m_PosSlotDecoder = new BitTreeDecoder[4];

		private BitDecoder[] m_PosDecoders = new BitDecoder[0x65496884 ^ 0x654968F6];

		private BitTreeDecoder m_PosAlignDecoder = new BitTreeDecoder(4);

		private LenDecoder m_LenDecoder = new LenDecoder();

		private LenDecoder m_RepLenDecoder = new LenDecoder();

		private LiteralDecoder m_LiteralDecoder = new LiteralDecoder();

		private uint m_DictionarySize;

		private uint m_DictionarySizeCheck;

		private uint m_PosStateMask;

		private bool _solid;

		public LzmaDecoder()
		{
			m_DictionarySize = uint.MaxValue;
			for (int i = 0; (long)i < 4L; i++)
			{
				ref BitTreeDecoder reference = ref m_PosSlotDecoder[i];
				reference = new BitTreeDecoder(6);
			}
		}

		private void SetDictionarySize(uint dictionarySize)
		{
			if (m_DictionarySize != dictionarySize)
			{
				m_DictionarySize = dictionarySize;
				m_DictionarySizeCheck = Math.Max(m_DictionarySize, 1u);
				uint windowSize = Math.Max(m_DictionarySizeCheck, 4096u);
				m_OutWindow.Create(windowSize);
			}
		}

		private void SetLiteralProperties(int lp, int lc)
		{
			m_LiteralDecoder.Create(lp, lc);
		}

		private void SetPosBitsProperties(int pb)
		{
			uint num = (uint)(1 << (pb & 0x1F));
			m_LenDecoder.Create(num);
			m_RepLenDecoder.Create(num);
			m_PosStateMask = num - 1;
		}

		private void Init(Stream inStream, Stream outStream)
		{
			m_RangeDecoder.Init(inStream);
			m_OutWindow.Init(outStream, _solid);
			for (uint num = 0u; num < 12; num++)
			{
				for (uint num2 = 0u; num2 <= m_PosStateMask; num2++)
				{
					uint num3 = (num << 4) + num2;
					m_IsMatchDecoders[num3].Init();
					m_IsRep0LongDecoders[num3].Init();
				}
				m_IsRepDecoders[num].Init();
				m_IsRepG0Decoders[num].Init();
				m_IsRepG1Decoders[num].Init();
				m_IsRepG2Decoders[num].Init();
			}
			m_LiteralDecoder.Init();
			for (uint num = 0u; num < 4; num++)
			{
				m_PosSlotDecoder[num].Init();
			}
			for (uint num = 0u; num < (0x2390DEFD ^ 0x2390DE8F); num++)
			{
				m_PosDecoders[num].Init();
			}
			m_LenDecoder.Init();
			m_RepLenDecoder.Init();
			m_PosAlignDecoder.Init();
		}

		public void Code(Stream inStream, Stream outStream, long inSize, long outSize)
		{
			Init(inStream, outStream);
			State state = default(State);
			state.Init();
			uint num = 0u;
			uint num2 = 0u;
			uint num3 = 0u;
			uint num4 = 0u;
			ulong num5 = 0uL;
			if (num5 < (ulong)outSize)
			{
				if (m_IsMatchDecoders[state.Index << 4].Decode(m_RangeDecoder) != 0)
				{
					throw new Exception();
				}
				state.UpdateChar();
				byte b = m_LiteralDecoder.DecodeNormal(m_RangeDecoder, 0u, 0);
				m_OutWindow.PutByte(b);
				num5++;
			}
			while (num5 < (ulong)outSize)
			{
				uint num6 = (uint)(int)num5 & m_PosStateMask;
				if (m_IsMatchDecoders[(state.Index << 4) + num6].Decode(m_RangeDecoder) == 0)
				{
					byte @byte = m_OutWindow.GetByte(0u);
					byte b2 = (state.IsCharState() ? m_LiteralDecoder.DecodeNormal(m_RangeDecoder, (uint)num5, @byte) : m_LiteralDecoder.DecodeWithMatchByte(m_RangeDecoder, (uint)num5, @byte, m_OutWindow.GetByte(num)));
					m_OutWindow.PutByte(b2);
					state.UpdateChar();
					num5++;
					continue;
				}
				uint num8;
				if (m_IsRepDecoders[state.Index].Decode(m_RangeDecoder) == 1)
				{
					if (m_IsRepG0Decoders[state.Index].Decode(m_RangeDecoder) == 0)
					{
						if (m_IsRep0LongDecoders[(state.Index << 4) + num6].Decode(m_RangeDecoder) == 0)
						{
							state.UpdateShortRep();
							m_OutWindow.PutByte(m_OutWindow.GetByte(num));
							num5++;
							continue;
						}
					}
					else
					{
						uint num7;
						if (m_IsRepG1Decoders[state.Index].Decode(m_RangeDecoder) == 0)
						{
							num7 = num2;
						}
						else
						{
							if (m_IsRepG2Decoders[state.Index].Decode(m_RangeDecoder) == 0)
							{
								num7 = num3;
							}
							else
							{
								num7 = num4;
								num4 = num3;
							}
							num3 = num2;
						}
						num2 = num;
						num = num7;
					}
					num8 = m_RepLenDecoder.Decode(m_RangeDecoder, num6) + 2;
					state.UpdateRep();
				}
				else
				{
					num4 = num3;
					num3 = num2;
					num2 = num;
					num8 = 2 + m_LenDecoder.Decode(m_RangeDecoder, num6);
					state.UpdateMatch();
					uint num9 = m_PosSlotDecoder[GetLenToPosState(num8)].Decode(m_RangeDecoder);
					if (num9 >= 4)
					{
						int num10 = (int)((num9 >> 1) - 1);
						num = (2 | (num9 & 1)) << (num10 & 0x1F);
						if (num9 < 14)
						{
							num += BitTreeDecoder.ReverseDecode(m_PosDecoders, num - num9 - 1, m_RangeDecoder, num10);
						}
						else
						{
							num += m_RangeDecoder.DecodeDirectBits(num10 - 4) << 4;
							num += m_PosAlignDecoder.ReverseDecode(m_RangeDecoder);
						}
					}
					else
					{
						num = num9;
					}
				}
				if ((num >= num5 || num >= m_DictionarySizeCheck) && num == uint.MaxValue)
				{
					break;
				}
				m_OutWindow.CopyBlock(num, num8);
				num5 += num8;
			}
			m_OutWindow.Flush();
			m_OutWindow.ReleaseStream();
			m_RangeDecoder.ReleaseStream();
		}

		public void SetDecoderProperties(byte[] properties)
		{
			int lc = properties[0] % 9;
			int num = properties[0] / 9;
			int lp = num % 5;
			int posBitsProperties = num / 5;
			uint num2 = 0u;
			for (int i = 0; i < 4; i++)
			{
				num2 += (uint)(properties[1 + i] << ((i * 8) & 0x1F));
			}
			SetDictionarySize(num2);
			SetLiteralProperties(lp, lc);
			SetPosBitsProperties(posBitsProperties);
		}
	}

	private static string Res = "亘\uefe1뢬䤳ᘠꎒ隤䫦";

	private static ulong Rid = 127351190uL;

	private static Module Mod;

	public static uint GetLenToPosState(uint len)
	{
		len -= 2;
		if (len < 4)
		{
			return len;
		}
		return 3u;
	}

	private static Assembly DecryptAsm(object sender, ResolveEventArgs e)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(e.Name);
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = (byte)(bytes[i] ^ 0xBu ^ (uint)i);
		}
		string @string = Encoding.UTF8.GetString(bytes);
		Stream manifestResourceStream = typeof(CompressShell).Assembly.GetManifestResourceStream(@string);
		if (manifestResourceStream != null)
		{
			byte[] asm;
			using (BinaryReader binaryReader = new BinaryReader(manifestResourceStream))
			{
				asm = binaryReader.ReadBytes((int)manifestResourceStream.Length);
			}
			asm = Decrypt(asm);
            //File.WriteAllBytes("c:\\maicjadir\\watykan.exe", asm);  tu zapisywalem dlla
            //POCZATEK LADOWANIA DLL
            Assembly result = Assembly.Load(asm);
			byte[] src = new byte[asm.Length];
			Buffer.BlockCopy(src, 0, asm, 0, asm.Length);
			return result;
			//koniec
			return null;
		}
		return null;
	}

	private static Assembly ResolveResource(object sender, ResolveEventArgs e)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(e.Name);
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = (byte)(bytes[i] ^ 0x36 ^ i);
		}
		string @string = Encoding.UTF8.GetString(bytes);
		Stream manifestResourceStream = typeof(CompressShell).Assembly.GetManifestResourceStream(@string);
		if (manifestResourceStream != null)
		{
			byte[] asm;
			using (BinaryReader binaryReader = new BinaryReader(manifestResourceStream))
			{
				asm = binaryReader.ReadBytes((int)manifestResourceStream.Length);
			}
			asm = Decrypt(asm);
			Assembly result = Assembly.Load(asm);
			byte[] src = new byte[asm.Length];
			Buffer.BlockCopy(src, 0, asm, 0, asm.Length);
			return result;
		}
		return null;
	}

	private static byte[] Decrypt(byte[] asm)
	{
		byte[] buffer;
		byte[] rgbIV;
		byte[] array;
		using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(asm)))
		{
			buffer = binaryReader.ReadBytes(binaryReader.ReadInt32());
			rgbIV = binaryReader.ReadBytes(binaryReader.ReadInt32());
			array = binaryReader.ReadBytes(binaryReader.ReadInt32());
		}
		int num = 90;
		for (int i = 0; i < array.Length; i += 4)
		{
			array[i] ^= (byte)(num & 0xFF);
			array[i + 1] ^= (byte)((num & 0xFF00) >> 8);
			array[i + 2] ^= (byte)((num & 0xFF0000) >> 16);
			array[i + 3] ^= (byte)((num & 0xFF000000u) >> 1519428737 - 1519428713);
		}
		RijndaelManaged rijndaelManaged = new RijndaelManaged();
		using CryptoStream cryptoStream = new CryptoStream(new MemoryStream(buffer), rijndaelManaged.CreateDecryptor(array, rgbIV), CryptoStreamMode.Read);
		byte[] array2 = new byte[4];
		cryptoStream.Read(array2, 0, 4);
		uint num2 = BitConverter.ToUInt32(array2, 0);
		LzmaDecoder lzmaDecoder = new LzmaDecoder();
		byte[] array3 = new byte[5];
		cryptoStream.Read(array3, 0, 5);
		lzmaDecoder.SetDecoderProperties(array3);
		long num3 = 0L;
		for (int j = 0; j < 8; j++)
		{
			int num4 = cryptoStream.ReadByte();
			if (num4 < 0)
			{
				throw new Exception("Can't Read 1");
			}
			num3 |= (long)((ulong)(byte)num4 << ((8 * j) & 0x3F));
		}
		byte[] array4 = new byte[num3];
		long inSize = num2 - 13;
		lzmaDecoder.Code(cryptoStream, new MemoryStream(array4, writable: true), inSize, num3);
		return array4;
	}

	private static ulong modPow(ulong bas, ulong pow, ulong mod)
	{
		ulong num = 1uL;
		while (pow != 0)
		{
			if ((pow & 1) != 0)
			{
				num = num * bas % mod;
			}
			pow >>= 1;
			bas = bas * bas % mod;
		}
		return num;
	}

	[STAThread]
	private static int Main(string[] args)
	{
		Assembly entryAssembly = Assembly.GetEntryAssembly();
		Stream manifestResourceStream = entryAssembly.GetManifestResourceStream(Res);
		byte[] asm;
		using (BinaryReader binaryReader = new BinaryReader(manifestResourceStream))
		{
			asm = binaryReader.ReadBytes((int)manifestResourceStream.Length);
		}
		asm = Decrypt(asm);
        //File.WriteAllBytes("c:\\maicjadir\\prewatykan.exe", asm); tu zapisywalem dlla
        Mod = entryAssembly.LoadModule("___.netmodule", asm);
		byte[] src = new byte[asm.Length];
		Buffer.BlockCopy(src, 0, asm, 0, asm.Length);
        //File.WriteAllBytes("c:\\maicjadir\\prewatykansrc.exe", src);  tu zapisywalem dlla
        AppDomain.CurrentDomain.AssemblyResolve += DecryptAsm;
		//TU ZACZYNA SIE ODPALANIE DLL Z RESOURCOW
		MethodBase methodBase = Mod.ResolveMethod(0x6000000 | (int)modPow(Rid, 71uL, 493482433uL));
		object obj = ((methodBase.GetParameters().Length != 1) ? methodBase.Invoke(null, null) : methodBase.Invoke(null, new object[1] { args }));
		if (obj is int)
		{
			return (int)obj;
		}
		//tu koniec
		return 0;
	}
}
