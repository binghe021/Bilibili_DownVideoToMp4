using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bilibili_DownVideoToMp4
{
 

    /// <summary>
    /// json对象
    /// </summary>
    public class EntryModel
    {
        public int media_type { get; set; }
        public bool has_dash_audio { get; set; }
        public bool is_completed { get; set; }
        public int total_bytes { get; set; }
        public int downloaded_bytes { get; set; }
        public string title { get; set; }
        public string type_tag { get; set; }
        public string cover { get; set; }
        public int prefered_video_quality { get; set; }
        public int guessed_total_bytes { get; set; }
        public int total_time_milli { get; set; }
        public int danmaku_count { get; set; }
        public long time_update_stamp { get; set; }
        public long time_create_stamp { get; set; }
        public int avid { get; set; }
        public int spid { get; set; }
        public int seasion_id { get; set; }
        public Page_Data page_data { get; set; }
    }

    /// <summary>
    /// 详情页对象
    /// </summary>
    public class Page_Data
    {
        public int cid { get; set; }
        public int page { get; set; }
        public string from { get; set; }
        public string part { get; set; }
        public string vid { get; set; }
        public bool has_alias { get; set; }
        public int tid { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int rotate { get; set; }
        /// <summary>
        /// 下载标题
        /// </summary>
        public string download_title { get; set; }
        /// <summary>
        /// 下载子标题
        /// </summary>
        public string download_subtitle { get; set; }
    }

}
