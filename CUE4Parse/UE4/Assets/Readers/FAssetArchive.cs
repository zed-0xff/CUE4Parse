﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using CUE4Parse.UE4.Assets.Utils;
using CUE4Parse.UE4.Exceptions;
using CUE4Parse.UE4.Objects.UObject;
using CUE4Parse.UE4.Readers;

namespace CUE4Parse.UE4.Assets.Readers
{
    public class FAssetArchive : FArchive
    {
        private readonly FArchive _baseArchive;
        public readonly IPackage Owner;
        public int AbsoluteOffset;

        public bool HasUnversionedProperties => Owner.HasFlags(PackageFlags.UnversionedProperties);

        private readonly Dictionary<PayloadType, Lazy<FAssetArchive?>> _payloads = new Dictionary<PayloadType, Lazy<FAssetArchive?>>();

        public FAssetArchive(FArchive baseArchive, IPackage owner, int absoluteOffset = 0)
        {
            _baseArchive = baseArchive;
            Owner = owner;
            AbsoluteOffset = absoluteOffset;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FName ReadFName()
        {
            var nameIndex = Read<int>();
            var extraIndex = Read<int>();
#if !NO_FNAME_VALIDATION
            if (nameIndex < 0 || nameIndex >= Owner.NameMap.Length)
            {
                throw new ParserException(this, $"FName could not be read, requested index {nameIndex}, name map size {Owner.NameMap.Length}");
            }            
#endif
            return new FName(Owner.NameMap[nameIndex], nameIndex, extraIndex);
        }

        public bool TryGetPayload(PayloadType type, out FAssetArchive? ar)
        {
            ar = null;
            if (!_payloads.TryGetValue(type, out var ret)) return false;
            
            ar = ret.Value;
            return true;
        }

        public FAssetArchive GetPayload(PayloadType type)
        {
            _payloads.TryGetValue(type, out var ret);
            var reader = ret?.Value;
            return reader ?? throw new ParserException(this, $"{type} is needed to parse the current package");
        }

        public void AddPayload(PayloadType type, FAssetArchive payload)
        {
            if (_payloads.ContainsKey(type))
            {
                throw new ParserException(this, $"Can't add a payload that is already attached of type {type}");
            }

            _payloads[type] = new Lazy<FAssetArchive?>(() => payload);
        }
        
        public void AddPayload(PayloadType type, int absoluteOffset, Lazy<FArchive?> payload)
        {
            if (_payloads.ContainsKey(type))
            {
                throw new ParserException(this, $"Can't add a payload that is already attached of type {type}");
            }
            _payloads[type] = new Lazy<FAssetArchive?>(() =>
            {
                var rawAr = payload.Value;
                return rawAr == null ? null : new FAssetArchive(rawAr, Owner, absoluteOffset);
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int Read(byte[] buffer, int offset, int count)
            => _baseArchive.Read(buffer, offset, count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long Seek(long offset, SeekOrigin origin)
            => _baseArchive.Seek(offset, origin);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long SeekAbsolute(long offset, SeekOrigin origin)
            => _baseArchive.Seek(offset - AbsoluteOffset, origin);
        
        public override bool CanSeek => _baseArchive.CanSeek;
        public override long Length => _baseArchive.Length;
        public long AbsolutePosition => AbsoluteOffset + Position;
        public override long Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _baseArchive.Position;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _baseArchive.Position = value;
        }

        public override string Name => _baseArchive.Name;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T Read<T>()
            => _baseArchive.Read<T>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte[] ReadBytes(int length)
            => _baseArchive.ReadBytes(length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override unsafe void Read(byte* ptr, int length)
            => _baseArchive.Read(ptr, length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override T[] ReadArray<T>(int length)
            => _baseArchive.ReadArray<T>(length);

        public override object Clone() => new FAssetArchive((FArchive) _baseArchive.Clone(), Owner, AbsoluteOffset);
    }
}