using System.Buffers.Binary;
using System.Text;

internal static class ResourceIndexTests
{
    private sealed record ObjectBlob(long Id, int ClassId, byte[] Data);
    private sealed record WrittenAsset(byte[] Bytes, Dictionary<long, int> RecordOffsets);

    internal static void Run(Action<bool, string> check, string? fullGameData)
    {
        string directory = Path.Combine(Path.GetTempPath(), "UEI.ResourceIndex.Tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string managerPath = Path.Combine(directory, "globalgamemanagers");
            string resourcePath = Path.Combine(directory, "resources.assets");
            var blobs = new[]
            {
                new ObjectBlob(100, 1, [1]), // Canonical crate GameObject.
                new ObjectBlob(200, 1, [2]), // Embedded same-name preset, not a resource entry.
                new ObjectBlob(201, 1, [3]), // Canonical parent containing source children.
                new ObjectBlob(300, 213, [4]), // Sprite; never queued as a prefab.
                new ObjectBlob(400, 114, [5]), // Managed component; metadata includes its script GUID.
            };
            WrittenAsset resources = WriteAsset(resourcePath, blobs, []);
            byte[] map = ResourceMap(("medcrate", 1, 100), ("structures/lifepod", 1, 201), ("icons/crate", 1, 300));
            WrittenAsset manager = WriteAsset(managerPath, [new(13, 147, map)], ["resources.assets"]);
            var result = UeiResourceIndex.Read(directory);
            check(result.Paths.SequenceEqual(new[] { "medcrate", "structures/lifepod" }) && result.UnavailableFiles.Length == 0,
                "The resource reader must select canonical GameObject references, exclude sprites and ignore embedded preset variants");

            // Non-existent external files produce honest partial coverage while
            // retaining paths from asset files that can be read.
            WriteAsset(managerPath, [new(13, 147, ResourceMap(("medcrate", 1, 100), ("missing", 2, 999)))], ["resources.assets", "missing.assets"]);
            result = UeiResourceIndex.Read(directory);
            check(result.Paths.SequenceEqual(new[] { "medcrate" }) && result.UnavailableFiles.SequenceEqual(new[] { "missing.assets" }),
                "Unavailable external metadata must retain known canonical paths and report partial coverage");

            WriteAsset(managerPath, [new(13, 147, ResourceMap(("escape", 1, 100)))], ["../outside.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "External asset paths must never escape the game data directory");
            WriteAsset(managerPath, [new(13, 147, ResourceMap(("../escape", 1, 100)))], ["resources.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Invalid relative Unity resource paths must be rejected");
            WriteAsset(managerPath, [new(13, 147, ResourceMap(("medcrate", 2, 100)))], ["resources.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Out-of-range resource file IDs must be rejected");
            WriteAsset(managerPath, [new(13, 147, ResourceMap(("medcrate", 1, 999)))], ["resources.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Missing resource object IDs must not be guessed from names");

            byte[] invalidCount = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(invalidCount, -1);
            WriteAsset(managerPath, [new(13, 147, invalidCount)], ["resources.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Negative resource counts must be rejected without allocating collections");
            BinaryPrimitives.WriteInt32LittleEndian(invalidCount, 100001);
            WriteAsset(managerPath, [new(13, 147, invalidCount)], ["resources.assets"]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Unbounded resource counts must be rejected");
            WriteAsset(managerPath, [new(13, 147, ResourceMap(("medcrate", 1, 100)))], ["resources.assets"]);
            byte[] badRecord = (byte[])manager.Bytes.Clone();
            BinaryPrimitives.WriteInt32LittleEndian(badRecord.AsSpan(manager.RecordOffsets[13] + 20), 999);
            File.WriteAllBytes(managerPath, badRecord);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Invalid object type indices must be rejected");
            badRecord = (byte[])manager.Bytes.Clone();
            BinaryPrimitives.WriteInt64LittleEndian(badRecord.AsSpan(manager.RecordOffsets[13] + 8), long.MaxValue);
            File.WriteAllBytes(managerPath, badRecord);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Object offsets outside the serialized file must be rejected without overflow");

            byte[] unsupported = (byte[])manager.Bytes.Clone();
            BinaryPrimitives.WriteUInt32BigEndian(unsupported.AsSpan(8), 21);
            File.WriteAllBytes(managerPath, unsupported);
            check(Rejects<NotSupportedException>(() => UeiResourceIndex.Read(directory)), "Unknown Unity file formats must fail explicitly instead of assuming a layout");
            unsupported = (byte[])manager.Bytes.Clone();
            unsupported[48 + Encoding.UTF8.GetByteCount("2022.3.62f3") + 1 + 4] = 1;
            File.WriteAllBytes(managerPath, unsupported);
            check(Rejects<NotSupportedException>(() => UeiResourceIndex.Read(directory)), "Type-tree asset files must fail explicitly");
            File.WriteAllBytes(managerPath, [0, 1, 2]);
            check(Rejects<EndOfStreamException>(() => UeiResourceIndex.Read(directory)), "Truncated Unity headers must fail within their read bounds");
            File.WriteAllBytes(managerPath, manager.Bytes);
            File.WriteAllBytes(resourcePath, resources.Bytes[..^1]);
            check(Rejects<InvalidDataException>(() => UeiResourceIndex.Read(directory)), "Truncated referenced assets must not silently provide a complete index");
        }
        finally
        {
            string resolved = Path.GetFullPath(directory);
            if (Path.GetDirectoryName(resolved) != Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar)
                || !Path.GetFileName(resolved).StartsWith("UEI.ResourceIndex.Tests-", StringComparison.Ordinal))
                throw new InvalidOperationException("Refusing to clean up a path outside the resource-test directory.");
            Directory.Delete(resolved, recursive: true);
        }
        Console.WriteLine("PASS: bounded Unity metadata reader, canonical resource paths, partial coverage and unsupported/malformed files");

        if (!string.IsNullOrWhiteSpace(fullGameData))
        {
            var current = UeiResourceIndex.Read(fullGameData);
            string[] known = ["medcrate", "foodbox", "dropcapsule", "lifepodchest", "containercrate", "lifepod"];
            check(current.Paths.Length > 100 && known.All(key => current.Paths.Contains(key, StringComparer.OrdinalIgnoreCase)),
                "Current full-game metadata must resolve the verified canonical crate/capsule/parent resource paths");
            check(current.UnavailableFiles.Length == 0, "Current full-game resources should have complete readable metadata");
            Console.WriteLine($"PASS: current full-game metadata: {current.Paths.Length} GameObject paths; no Unity assets loaded");
        }
    }

    private static bool Rejects<T>(Action action) where T : Exception
    {
        try { action(); return false; }
        catch (T) { return true; }
    }

    internal static void WriteDriverFixture(string directory, string[] keys)
    {
        Directory.CreateDirectory(directory);
        WriteAsset(Path.Combine(directory, "resources.assets"), keys.Select((_, i) => new ObjectBlob(i + 1, 1, [1])).ToArray(), []);
        WriteAsset(Path.Combine(directory, "globalgamemanagers"), [new(13, 147,
            ResourceMap(keys.Select((key, i) => (key, 1, (long)i + 1)).ToArray()))], ["resources.assets"]);
    }

    private static byte[] ResourceMap(params (string Key, int File, long Id)[] entries)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream, Encoding.UTF8, leaveOpen: true);
        writer.Write(entries.Length);
        foreach (var entry in entries)
        {
            byte[] key = Encoding.UTF8.GetBytes(entry.Key);
            writer.Write(key.Length);
            writer.Write(key);
            Align(writer, 4);
            writer.Write(entry.File);
            writer.Write(entry.Id);
        }
        return stream.ToArray();
    }

    private static WrittenAsset WriteAsset(string path, ObjectBlob[] objects, string[] externals)
    {
        int[] classes = objects.Select(x => x.ClassId).Distinct().ToArray();
        using MemoryStream metadata = new();
        using BinaryWriter writer = new(metadata, Encoding.UTF8, leaveOpen: true);
        CString(writer, "2022.3.62f3");
        writer.Write(19); // Windows player.
        writer.Write((byte)0); // Type trees disabled.
        writer.Write(classes.Length);
        foreach (int classId in classes)
        {
            writer.Write(classId);
            writer.Write((byte)0);
            writer.Write((short)-1);
            if (classId == 114) writer.Write(new byte[16]);
            writer.Write(new byte[16]);
        }
        writer.Write(objects.Length);
        var offsets = new Dictionary<long, int>();
        long position = 0;
        foreach (var obj in objects)
        {
            Align(writer, 4);
            offsets.Add(obj.Id, 48 + (int)metadata.Position);
            writer.Write(obj.Id);
            writer.Write(position);
            writer.Write((uint)obj.Data.Length);
            writer.Write(Array.IndexOf(classes, obj.ClassId));
            position += obj.Data.Length;
        }
        writer.Write(0); // Script reference count.
        writer.Write(externals.Length);
        foreach (string external in externals)
        {
            CString(writer, "");
            writer.Write(new byte[20]);
            CString(writer, external);
        }
        byte[] meta = metadata.ToArray();
        int dataOffset = (48 + meta.Length + 15) & ~15;
        int fileSize = dataOffset + objects.Sum(x => x.Data.Length);
        byte[] file = new byte[fileSize];
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(0), (uint)meta.Length);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(4), (uint)fileSize);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(8), 22);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(12), (uint)dataOffset);
        BinaryPrimitives.WriteUInt32BigEndian(file.AsSpan(20), (uint)meta.Length);
        BinaryPrimitives.WriteInt64BigEndian(file.AsSpan(24), fileSize);
        BinaryPrimitives.WriteInt64BigEndian(file.AsSpan(32), dataOffset);
        meta.CopyTo(file, 48);
        int start = dataOffset;
        foreach (var obj in objects) { obj.Data.CopyTo(file, start); start += obj.Data.Length; }
        File.WriteAllBytes(path, file);
        return new(file, offsets);
    }

    private static void CString(BinaryWriter writer, string value)
    {
        writer.Write(Encoding.UTF8.GetBytes(value));
        writer.Write((byte)0);
    }
    private static void Align(BinaryWriter writer, int alignment)
    {
        while (writer.BaseStream.Position % alignment != 0) writer.Write((byte)0);
    }
}
