﻿using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Exceptions;
using System;
using System.Collections.Generic;
using Serilog;

namespace CUE4Parse.UE4.Assets.Objects
{
    public class UScriptArray
    {
        public readonly string InnerType; 
        public readonly FPropertyTagData? InnerTagData;
        public readonly List<FPropertyTagType> Properties;

        public UScriptArray(string innerType)
        {
            InnerType = innerType;
            InnerTagData = null;
            Properties = new List<FPropertyTagType>();
        }

        public UScriptArray(FAssetArchive Ar, FPropertyTagData? tagData)
        {
            InnerType = tagData?.InnerType ?? throw new ParserException(Ar, "UScriptArray needs inner type");
            var elementCount = Ar.Read<int>();
            if (Ar.HasUnversionedProperties)
            {
                InnerTagData = tagData.InnerTypeData;
            }
            else if (InnerType == "StructProperty" || InnerType == "ArrayProperty")
            {
                InnerTagData = new FPropertyTag(Ar, false).TagData;
                if (InnerTagData == null)
                    throw new ParserException(Ar, $"Couldn't read ArrayProperty with inner type {InnerType}");
            }

            Properties = new List<FPropertyTagType>(elementCount);
            for (int i = 0; i < elementCount; i++)
            {
                var property = FPropertyTagType.ReadPropertyTagType(Ar, InnerType, InnerTagData, ReadType.ARRAY);
                if (property != null)
                    Properties.Add(property);
                else
                    Log.Debug(
                        $"Failed to read array property of type {InnerType} at ${Ar.Position}, index {i}");
            }
        }

        public override string ToString() => $"{InnerType}[{Properties.Count}]";
    }
}
