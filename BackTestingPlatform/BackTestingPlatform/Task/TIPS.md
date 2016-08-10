﻿# 常用代码提示 USUAL CODING TIPS 

## 获取appSetting里的配置
```
string value = System.Configuration.ConfigurationManager.AppSettings["myKey"];
```

## 字符串,数值，日期之间的转换
```
//使用原生api,字符串转换为日期
DateTime dt=DateTime.ParseExact(str, "yyyyMMddhhmmss", CultureInfo.InvariantCulture);

//使用自定义函数，请参见Utilities下的Kit.cs,例如:
DateTime dt=Kit.ToDateTime("20160805");			//字符串 -> 日期
DateTime dt=Kit.ToDateTime("20160805134545");	//字符串 -> 日期
DateTime dt=Kit.ToDateTime(20160805134545);		//数字 -> 日期
DateTime dt=Kit.ToDateTime("20160809","094500");//字符串 -> 日期
DateTime dt=Kit.ToDateTime(20160809,94500);		//数字 -> 日期

int x=Kit.ToInt_yyyyMMdd(dt);					//日期 -> 数字，例如20160305	

int a=Kit.ToInt(anObject);						//decimal,double,string等类型 -> int, 比Convert.ToInt32(object)更为安全
int b=Kit.ToDouble(anObject);					//decimal,double,string等类型 -> double, 比Convert.ToDouble(object)更为安全
```

## 获取/寄存全局缓存数据
```
List<DateTime> tradeDays = Caches.get<List<DateTime>>("TradeDays");		//获取TradeDays
MyModel m = Caches.get<MyModel>("MyModelKey");							//获取MyModelKey
Caches.put("MyKey",tradeDays);											//寄存，如果key已存在则覆盖
```

## 获取Service,Repository等实例（全局共享的唯一实例，由autofac托管）。
```
    TradeDaysService tradeDaysService = Platforms.container.Resolve<TradeDaysService>();
    TradeDaysRepository tradeDaysRepository = Platforms.container.Resolve<TradeDaysRepository>();
```

