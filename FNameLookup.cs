using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Voxif.Memory;

namespace LiveSplit.WekoMaskGatherer;

public sealed class FNameLookup {
    private const int TagSize = 6;
    private const int TagMask = (1 << TagSize) - 1;
    private const int Utf16Tag = 1;
    private const int MaxBytesToFetch = 0x2_0000;
    public const int MaxStringLength = (ushort.MaxValue >> TagSize);

    private struct Chunk {
        public byte[]? Data;
        public ushort LoadedOffset;
    }

    private Chunk[] _chunks;
    private readonly Dictionary<FNameEntryId, string> _lookup;
    private readonly GameAddressHandle<IntPtr> _handle;

    public FNameLookup(GameAddressHandle<IntPtr> handle) {
        _chunks = Array.Empty<Chunk>();
        _lookup = new Dictionary<FNameEntryId, string>();
        _handle = handle;
    }


    public string FindString(FNameEntryId fName) {
        if (_lookup.TryGetValue(fName, out var value)) return value;
        if(!_handle.IsGameRunning) return fName.ToString();

        if (_chunks.Length <= fName.Chunk) {
            GrowTable(fName);
        }
        if (_chunks[fName.Chunk].LoadedOffset <= fName.Offset) {
            FetchChunk(fName);
        }

        // Chunk is guaranteed to have been set by FetchChunk()
        var chunk = _chunks[fName.Chunk].Data!;
        var offset = fName.Offset * 2;
        var header = chunk.To<ushort>(offset);
        var size = header >> TagSize;
        string text;
        if((header & Utf16Tag) == 0) {
            text = Encoding.UTF8.GetString(chunk, offset + 2, size);
        } else {
            text = Encoding.Unicode.GetString(chunk, offset + 2, size * 2);
        }
        _lookup[fName] = text;
        return text;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowTable(FNameEntryId fName) {
        var oldTable = _chunks;
        var grow = Math.Max(oldTable.Length * 2, fName.Chunk + 1);
        _chunks = new Chunk[grow];
        Array.Copy(oldTable, _chunks, oldTable.Length);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void FetchChunk(FNameEntryId fName) {
        ref var chunk = ref _chunks[fName.Chunk];
        if (chunk.Data is null) {
            chunk.Data = new byte[MaxBytesToFetch];
        }

        var chunkTable = _handle.Address.New;
        var game = _handle.Game;
        var tableOffset = fName.Chunk * game.PointerSize;
        var chunkAddress = game.Read<IntPtr>(chunkTable, tableOffset);

        int chunkOffset;
        // if the next table exists, we know our table is completely filled,
        // otherwise, only read up to the string we were asked to fetch.
        var nextAddress = game.Read<IntPtr>(chunkTable, tableOffset + game.PointerSize);
        if (nextAddress == IntPtr.Zero) {
            chunkOffset = fName.Offset * 2;
            var nameHeader = game.Read<ushort>(chunkAddress + chunkOffset);
            chunkOffset += 2 + (nameHeader >> TagSize);
        } else {
            chunkOffset = MaxBytesToFetch;
        }

        var existingBytes = chunk.LoadedOffset * 2;
        var newData = game.Read(chunkAddress + existingBytes, chunkOffset - existingBytes);
        Array.Copy(newData, 0, chunk.Data, existingBytes, newData.Length);
        chunk.LoadedOffset = (ushort)Math.Min(chunkOffset/2, ushort.MaxValue);
    }

    private string FindNameDirect(FNameEntryId nameId) {
        if (!_handle.IsGameRunning) return nameId.ToString();
        var game = _handle.Game;
        var chunk = game.Read<IntPtr>(_handle.Address.New, nameId.Chunk * game.PointerSize);
        var nameOffset = nameId.Offset * 2;
        var nameHeader = game.Read<ushort>(chunk + nameOffset);
        var flags = nameHeader & TagMask;
        var type = (flags & 1) != 0 ? EStringType.UTF16 : EStringType.UTF8;
        var size = nameHeader >> TagSize;
        string value = game.ReadString(chunk + nameOffset + 2, size * ((int)type/2), type);
        return value;
    }

    public void Reset() {
        _lookup.Clear();
        Array.Clear(_chunks, 0, _chunks.Length);
    }
}