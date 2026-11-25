using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

// Read resource names and object types only. Gameplay fields are read later
// through Unity, on its main thread. This reader never instantiates an asset.
internal static class UeiResourceIndex
{
    internal sealed class Result
    {
        public readonly string[] Paths;
        public readonly string[] UnavailableFiles;
        public Result(string[] paths, string[] unavailableFiles)
        {
            Paths = paths;
            UnavailableFiles = unavailableFiles;
        }
    }

    private readonly struct ObjectRecord
    {
        public readonly int ClassId;
        public readonly long Offset;
        public readonly uint Size;
        public ObjectRecord(int classId, long offset, uint size)
        {
            ClassId = classId;
            Offset = offset;
            Size = size;
        }
    }

    private sealed class AssetFile
    {
        public readonly string Path;
        public readonly Dictionary<long, ObjectRecord> Objects;
        public readonly string[] Externals;
        public AssetFile(string path, Dictionary<long, ObjectRecord> objects, string[] externals)
        {
            Path = path;
            Objects = objects;
            Externals = externals;
        }
    }

    public static Result Read(string dataDirectory)
    {
        string directory = System.IO.Path.GetFullPath(dataDirectory);
        AssetFile manager = ReadMetadata(System.IO.Path.Combine(directory, "globalgamemanagers"));
        Dictionary<string, AssetFile?> files = new(StringComparer.OrdinalIgnoreCase)
        {
            [manager.Path] = manager,
        };
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> unavailable = new(StringComparer.OrdinalIgnoreCase);
        bool foundManager = false;
        foreach (ObjectRecord record in manager.Objects.Values)
        {
            if (record.ClassId != 147) continue; // Native ResourceManager.
            foundManager = true;
            using FileStream stream = File.OpenRead(manager.Path);
            stream.Position = record.Offset;
            using Reader reader = new(stream, record.Offset + record.Size);
            int count = reader.Count(100000);
            for (int i = 0; i < count; i++)
            {
                string key = reader.String();
                int fileId = reader.Int32();
                long pathId = reader.Int64();
                if (fileId < 0 || fileId > manager.Externals.Length)
                    throw new InvalidDataException("Invalid resource file reference.");
                if (pathId == 0) continue;
                AssetFile? target = manager;
                if (fileId != 0)
                {
                    string external = manager.Externals[fileId - 1];
                    string targetPath = ResolveExternal(directory, external);
                    if (!files.TryGetValue(targetPath, out target))
                    {
                        try { target = ReadMetadata(targetPath); }
                        catch (IOException) { target = null; }
                        catch (NotSupportedException) { target = null; }
                        catch (UnauthorizedAccessException) { target = null; }
                        files[targetPath] = target;
                    }
                    if (target == null) unavailable.Add(external);
                }
                if (target == null) continue;
                if (!target.Objects.TryGetValue(pathId, out ObjectRecord referenced))
                    throw new InvalidDataException("Resource object is missing from its asset file.");
                if (referenced.ClassId == 1) // GameObject; no textures, sounds, or scenes.
                {
                    ValidateResourcePath(key);
                    paths.Add(key);
                }
            }
        }
        if (!foundManager || paths.Count == 0)
            throw new InvalidDataException("No GameObject resource index was found.");
        string[] sorted = new string[paths.Count];
        paths.CopyTo(sorted);
        Array.Sort(sorted, StringComparer.OrdinalIgnoreCase);
        string[] errors = new string[unavailable.Count];
        unavailable.CopyTo(errors);
        Array.Sort(errors, StringComparer.OrdinalIgnoreCase);
        return new Result(sorted, errors);
    }

    private static AssetFile ReadMetadata(string path)
    {
        using FileStream stream = File.OpenRead(path);
        byte[] header = new byte[48];
        ReadExactly(stream, header);
        if (BigEndian32(header, 8) != 22 || header[16] != 0)
            throw new NotSupportedException("Unsupported Unity serialized-file format (expected 22, little-endian).");
        long metadataSize = BigEndian32(header, 20);
        long fileSize = BigEndian64(header, 24);
        long dataOffset = BigEndian64(header, 32);
        if (fileSize != stream.Length || metadataSize < 16 || metadataSize > 64 * 1024 * 1024
            || dataOffset < 48 || dataOffset > fileSize || metadataSize > dataOffset - 48)
            throw new InvalidDataException("Invalid Unity serialized-file header.");

        using Reader reader = new(stream, 48 + metadataSize);
        reader.CString(256); // Engine version.
        reader.Int32(); // Target platform.
        if (reader.Byte() != 0)
            throw new NotSupportedException("Unity type trees are not supported by this resource-index reader.");
        int typeCount = reader.Count(4096);
        int[] classIds = new int[typeCount];
        for (int i = 0; i < typeCount; i++)
        {
            classIds[i] = reader.Int32();
            reader.Byte(); // Stripped type.
            reader.Skip(2); // Script type index.
            if (classIds[i] == 114) reader.Skip(16); // Script GUID.
            reader.Skip(16); // Old type hash; no type-tree payload in this format.
        }

        int objectCount = reader.Count(1000000);
        Dictionary<long, ObjectRecord> objects = new(objectCount);
        for (int i = 0; i < objectCount; i++)
        {
            reader.Align();
            long pathId = reader.Int64();
            long relativeOffset = reader.Int64();
            uint size = reader.UInt32();
            int typeIndex = reader.Int32();
            if (typeIndex < 0 || typeIndex >= typeCount || relativeOffset < 0
                || relativeOffset > fileSize - dataOffset || size > fileSize - dataOffset - relativeOffset)
                throw new InvalidDataException("Invalid Unity object record.");
            if (objects.ContainsKey(pathId)) throw new InvalidDataException("Duplicate Unity object ID.");
            objects.Add(pathId, new ObjectRecord(classIds[typeIndex], dataOffset + relativeOffset, size));
        }

        int scripts = reader.Count(1000000);
        for (int i = 0; i < scripts; i++)
        {
            reader.Int32();
            reader.Align();
            reader.Int64();
        }
        int externalCount = reader.Count(4096);
        string[] externals = new string[externalCount];
        for (int i = 0; i < externalCount; i++)
        {
            reader.CString(16384);
            reader.Skip(20); // GUID and external type.
            externals[i] = reader.CString(16384);
        }
        return new AssetFile(path, objects, externals);
    }

    private static string ResolveExternal(string directory, string external)
    {
        string relative = external.Replace('/', System.IO.Path.DirectorySeparatorChar).Replace('\\', System.IO.Path.DirectorySeparatorChar);
        if (string.IsNullOrWhiteSpace(relative) || System.IO.Path.IsPathRooted(relative))
            throw new InvalidDataException("Invalid external asset path.");
        string resolved = System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, relative));
        string prefix = directory.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)
            + System.IO.Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("External asset path leaves the game data directory.");
        return resolved;
    }

    private static void ValidateResourcePath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.IndexOf('\\') >= 0 || key.IndexOf('\0') >= 0
            || key.StartsWith("/", StringComparison.Ordinal) || key.IndexOf(':') >= 0)
            throw new InvalidDataException("Invalid Unity resource path.");
        foreach (string segment in key.Split('/'))
            if (segment.Length == 0 || segment == "." || segment == "..")
                throw new InvalidDataException("Invalid Unity resource path segment.");
    }

    private static uint BigEndian32(byte[] data, int offset)
        => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    private static long BigEndian64(byte[] data, int offset)
        => unchecked((long)(((ulong)BigEndian32(data, offset) << 32) | BigEndian32(data, offset + 4)));

    private static void ReadExactly(Stream stream, byte[] data)
    {
        int offset = 0;
        while (offset < data.Length)
        {
            int read = stream.Read(data, offset, data.Length - offset);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
    }

    private sealed class Reader : IDisposable
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private readonly Stream _stream;
        private readonly BinaryReader _reader;
        private readonly long _end;
        public Reader(Stream stream, long end)
        {
            _stream = stream;
            _reader = new BinaryReader(stream, Utf8, leaveOpen: true);
            _end = end;
            if (end < stream.Position || end > stream.Length) throw new InvalidDataException("Invalid reader bounds.");
        }
        public byte Byte() { Require(1); return _reader.ReadByte(); }
        public int Int32() { Require(4); return _reader.ReadInt32(); }
        public uint UInt32() { Require(4); return _reader.ReadUInt32(); }
        public long Int64() { Require(8); return _reader.ReadInt64(); }
        public int Count(int max)
        {
            int count = Int32();
            if (count < 0 || count > max) throw new InvalidDataException("Invalid Unity collection count.");
            return count;
        }
        public void Skip(int bytes) { Require(bytes); _stream.Position += bytes; }
        public void Align() => Skip((int)((4 - (_stream.Position & 3)) & 3));
        public string String()
        {
            int length = Count(16384);
            Require(length);
            string value = Utf8.GetString(_reader.ReadBytes(length));
            Align();
            return value;
        }
        public string CString(int max)
        {
            List<byte> bytes = new();
            for (int i = 0; i < max; i++)
            {
                byte value = Byte();
                if (value == 0) return Utf8.GetString(bytes.ToArray());
                bytes.Add(value);
            }
            throw new InvalidDataException("Unterminated Unity metadata string.");
        }
        private void Require(int bytes)
        {
            if (bytes < 0 || bytes > _end - _stream.Position) throw new EndOfStreamException("Truncated Unity asset metadata.");
        }
        public void Dispose() => _reader.Dispose();
    }
}
