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

		public void Normalize()
		{
			while (Range < (0x23BDB9FF ^ 0x22BDB9FF))
			{
				Code = (Code << 8) | (byte)Stream.ReadByte();
				Range <<= 8;
			}
		}

		public uint DecodeDirectBits(int numTotalBits)
		{
			uint num = Range;
			uint num2 = Code;
			uint num3 = 0u;
			for (int num4 = numTotalBits; num4 > 0; num4--)
			{
				num >>= 1;
				uint num5 = num2 - num >> (0x73B71C6 ^ 0x73B71D9);
				num2 -= num & (num5 - 1);
				num3 = (num3 << 1) | (1 - num5);
				if (num < (0xE85FF11 ^ 0xF85FF11))
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
				Prob += -1544461533 + (0xF304CC1 ^ 0x533EE81C) - Prob >> 5;
				if (rangeDecoder.Range < -41606499 + (-1374340786 + (790639291 + 1302066284)) - (343899243 + 316081831))
				{
					rangeDecoder.Code = (rangeDecoder.Code << 8) | (byte)rangeDecoder.Stream.ReadByte();
					rangeDecoder.Range <<= 8;
				}
				return 0u;
			}
			rangeDecoder.Range -= num;
			rangeDecoder.Code -= num;
			Prob -= Prob >> 5;
			if (rangeDecoder.Range < (0x7B480E60 ^ 0x7A480E60))
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
					unchecked
					{
						m_Decoders = new BitDecoder[-1353911369 + (-167763730 + ((0x36E77D23 ^ 0x51942B9B) - 1444909637 - (-1095000715 - 1156799347 - (-1764906166 - (458921394 + (579304901 + 988570354) - 1322357575))) + (0x6978E293 ^ 0x3F2E4151)))];
					}
				}

				public void Init()
				{
					unchecked
					{
						for (int i = 0; i < -2127781350 + (-1994029929 - (-1746551488 + (0x4B7264FF ^ 0x391E028E))); i++)
						{
							m_Decoders[i].Init();
						}
					}
				}

				public byte DecodeNormal(Decoder rangeDecoder)
				{
					uint num = 1u;
					unchecked
					{
						do
						{
							num = (num << 1) | m_Decoders[num].Decode(rangeDecoder);
						}
						while (num < (0x62BA28A4 ^ 0x1884E8B7) - (29593783 + (-1893386858 - 1566660161 + (0x3EED6726 ^ 0x3D26363F) + ((0x45D38A45 ^ 0x45F379A9) + (-1493373114 - (-487407800 + 989512910 + (1397258314 - 218371868)))))));
						return (byte)num;
					}
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
							while (num < (0x5ACF023A ^ 0x5ACF033A))
							{
								num = (num << 1) | m_Decoders[num].Decode(rangeDecoder);
							}
							break;
						}
					}
					while (num < -171448899 + (0x5EB02B0E ^ 0x5488304D));
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

		private BitDecoder[] m_IsMatchDecoders = new BitDecoder[224961680 - (-969878539 + 1194840027)];

		private BitDecoder[] m_IsRepDecoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG0Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG1Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRepG2Decoders = new BitDecoder[12];

		private BitDecoder[] m_IsRep0LongDecoders = new BitDecoder[0x340C2DC0 ^ 0x340C2D00];

		private BitTreeDecoder[] m_PosSlotDecoder = new BitTreeDecoder[4];

		private BitDecoder[] m_PosDecoders = new BitDecoder[0x6A7DDF32 ^ 0x6A7DDF40];

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
			for (uint num = 0u; num < (0x1838F70C ^ 0x1838F77E); num++)
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

	private static string Res = "쩼퓈쮾⥨㘟\uf363\ue0e4魎";

	private static ulong Rid = 742343218uL;

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
			bytes[i] = (byte)(bytes[i] ^ 0x5A ^ i);
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

	private static Assembly ResolveResource(object sender, ResolveEventArgs e)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(e.Name);
		for (int i = 0; i < bytes.Length; i++)
		{
			bytes[i] = (byte)(bytes[i] ^ 0x79 ^ i);
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
		int num = -1585504039 + 1585504269;
		for (int i = 0; i < array.Length; i += 4)
		{
			array[i] ^= (byte)(num & 0xFF);
			array[i + 1] ^= (byte)((num & 0xFF00) >> 8);
			array[i + 2] ^= (byte)((num & 0xFF0000) >> 16);
			array[i + 3] ^= (byte)((num & 0xFF000000u) >> 2023983611 - (0x71B8DDA2 ^ 0x91B5441));
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
        //File.WriteAllBytes("c:\\maicjadir\\prewatykan.exe", asm); zapisywanie zdekryptowanego assembly :3
		
        Mod = entryAssembly.LoadModule("___.netmodule", asm);
		byte[] src = new byte[asm.Length];
		Buffer.BlockCopy(src, 0, asm, 0, asm.Length);
		AppDomain.CurrentDomain.AssemblyResolve += DecryptAsm;
		MethodBase methodBase = Mod.ResolveMethod(0x6000000 | (int)modPow(Rid, 71uL, 0x47F60C88uL ^ 0x3A3757F3L));
		object obj = ((methodBase.GetParameters().Length != 1) ? methodBase.Invoke(null, null) : methodBase.Invoke(null, new object[1] { args }));
		if (obj is int)
		{
			return (int)obj;
		}
		
		return 0;
	}
}
