﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AltV.Net.Client.Elements.Data;
using AltV.Net.Client.Elements.Interfaces;
using AltV.Net.Data;
using AltV.Net.Elements.Args;
using AltV.Net.Elements.Entities;
using AltV.Net.Native;
using AltV.Net.Shared.Elements.Entities;
using AltV.Net.Shared.Utils;

namespace AltV.Net.Client.Elements.Entities
{
    public class Entity : WorldObject, IEntity
    {
        private static IntPtr GetWorldObjectPointer(ICore core, IntPtr entityPointer)
        {
            unsafe
            {
                return core.Library.Shared.Entity_GetWorldObject(entityPointer);
            }
        }

        public IntPtr EntityNativePointer { get; private set; }
        public override IntPtr NativePointer => EntityNativePointer;

        public Entity(ICore core, IntPtr entityPointer, uint id, BaseObjectType type) : base(core, GetWorldObjectPointer(core, entityPointer), type, id)
        {
            EntityNativePointer = entityPointer;
        }

        public uint Model
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();
                    return Core.Library.Shared.Entity_GetModel(EntityNativePointer);
                }
            }
        }

        public IPlayer? NetworkOwner
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();
                    var ptr = Core.Library.Shared.Entity_GetNetOwner(EntityNativePointer);
                    if (ptr == IntPtr.Zero) return null;
                    return Alt.Core.PoolManager.Player.Get(ptr);
                }
            }
        }
        ISharedPlayer ISharedEntity.NetworkOwner => NetworkOwner!;

        public uint ScriptId
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();
                    return Core.Library.Client.Entity_GetScriptID(EntityNativePointer);
                }
            }
        }

        public SyncInfo SyncInfo
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();

                    var syncInfoPtr = IntPtr.Zero;
                    Core.Library.Client.Entity_GetSyncInfo(EntityNativePointer, &syncInfoPtr);

                    var syncInfo = Marshal.PtrToStructure<SyncInfoInternal>(syncInfoPtr).ToPublic();

                    Core.Library.Shared.FreeSyncInfo(syncInfoPtr);

                    return syncInfo;
                }
            }
        }

        public bool Spawned => ScriptId != 0;

        public Position Position
        {
            get
            {
                return base.Position;
            }
            set
            {
                unsafe
                {
                    CheckIfEntityExists();
                    var networkOwner = this.NetworkOwner;
                    if (networkOwner is null || networkOwner != Alt.LocalPlayer)
                    {
                        throw new InvalidDataException("Position can only be modified by the network owner of the entity");
                    }

                    this.Core.Library.Shared.WorldObject_SetPosition(this.WorldObjectNativePointer, value);
                }
            }
        }

        public Rotation Rotation
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();
                    var position = Rotation.Zero;
                    this.Core.Library.Shared.Entity_GetRotation(this.EntityNativePointer, &position);
                    return position;
                }
            }
            set
            {
                unsafe
                {
                    CheckIfEntityExists();
                    var networkOwner = this.NetworkOwner;
                    if (networkOwner is null || networkOwner != Alt.LocalPlayer)
                    {
                        throw new InvalidDataException("Rotation can only be modified by the network owner of the entity");
                    }

                    this.Core.Library.Shared.Entity_SetRotation(this.EntityNativePointer, value);
                }
            }
        }

        public void GetStreamSyncedMetaData(string key, out MValueConst value)
        {
            CheckIfEntityExists();
            unsafe
            {
                var stringPtr = MemoryUtils.StringToHGlobalUtf8(key);
                value = new MValueConst(Core, Core.Library.Shared.Entity_GetStreamSyncedMetaData(EntityNativePointer, stringPtr));
                Marshal.FreeHGlobal(stringPtr);
            }
        }

        public bool HasStreamSyncedMetaData(string key)
        {
            CheckIfEntityExists();
            unsafe
            {
                var stringPtr = MemoryUtils.StringToHGlobalUtf8(key);
                var result = Core.Library.Shared.Entity_HasStreamSyncedMetaData(EntityNativePointer, stringPtr);
                Marshal.FreeHGlobal(stringPtr);
                return result == 1;
            }
        }

        public bool GetStreamSyncedMetaData<T>(string key, out T result)
        {
            CheckIfEntityExists();
            GetStreamSyncedMetaData(key, out MValueConst mValue);
            using (mValue)
            {

                try
                {
                    result = Utils.GetCastedMValue<T>(mValue);
                    return true;
                }
                catch
                {
                    result = default;
                    return false;
                }
            }
        }

        public bool Frozen
        {
            get
            {
                unsafe
                {
                    CheckIfEntityExistsOrCached();
                    return Core.Library.Shared.Entity_IsFrozen(EntityNativePointer) == 1;
                }
            }
            set
            {
                unsafe
                {
                    CheckIfEntityExists();
                    Core.Library.Shared.Entity_SetFrozen(EntityNativePointer, value ? (byte) 1 : (byte) 0);
                }
            }
        }

        public override void CheckIfEntityExists()
        {
            CheckIfCallIsValid();
            if (Exists) return;

            throw new EntityRemovedException(this);
        }

        public override void SetCached(IntPtr cachedEntity)
        {
            this.EntityNativePointer = cachedEntity;
            base.SetCached(GetWorldObjectPointer(Core, cachedEntity));
        }
    }
}