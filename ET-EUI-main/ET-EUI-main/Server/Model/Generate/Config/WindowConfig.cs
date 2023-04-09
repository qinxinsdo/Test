using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using ProtoBuf;

namespace ET
{
    [ProtoContract]
    [Config]
    public partial class WindowConfigCategory : ProtoObject, IMerge
    {
        public static WindowConfigCategory Instance;
		
        [ProtoIgnore]
        [BsonIgnore]
        private Dictionary<int, WindowConfig> dict = new Dictionary<int, WindowConfig>();
		
        [BsonElement]
        [ProtoMember(1)]
        private List<WindowConfig> list = new List<WindowConfig>();
		
        public WindowConfigCategory()
        {
            Instance = this;
        }
        
        public void Merge(object o)
        {
            WindowConfigCategory s = o as WindowConfigCategory;
            this.list.AddRange(s.list);
        }
		
        public override void EndInit()
        {
            foreach (WindowConfig config in list)
            {
                config.EndInit();
                this.dict.Add(config.Id, config);
            }            
            this.AfterEndInit();
        }
		
        public WindowConfig Get(int id)
        {
            this.dict.TryGetValue(id, out WindowConfig item);

            if (item == null)
            {
                throw new Exception($"配置找不到，配置表名: {nameof (WindowConfig)}，配置id: {id}");
            }

            return item;
        }
		
        public bool Contain(int id)
        {
            return this.dict.ContainsKey(id);
        }

        public Dictionary<int, WindowConfig> GetAll()
        {
            return this.dict;
        }

        public WindowConfig GetOne()
        {
            if (this.dict == null || this.dict.Count <= 0)
            {
                return null;
            }
            return this.dict.Values.GetEnumerator().Current;
        }
    }

    [ProtoContract]
	public partial class WindowConfig: ProtoObject, IConfig
	{
		/// <summary>Id</summary>
		[ProtoMember(1)]
		public int Id { get; set; }
		/// <summary>类型</summary>
		[ProtoMember(2)]
		public int Type { get; set; }
		/// <summary>窗口名称</summary>
		[ProtoMember(3)]
		public string Name { get; set; }
		/// <summary>描述</summary>
		[ProtoMember(4)]
		public string Desc { get; set; }

	}
}
