using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Events;

namespace susalem.EasyDemo.Share
{
    // 定义一个事件：登录状态改变事件
    // bool 参数：true 表示登录，false 表示登出
    public class LoginStatusChangedEvent : PubSubEvent<bool>
    {

    }
}
