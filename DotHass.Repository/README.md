# dothass






1.DictionaryCacheHandle or SystemRuntimeCaching  �Լ�msmemorycache���ǲ������л���



//Ϊĳ������.�������û���

   services.AddCacheManager<CacheSession>(configure: builder =>
                {
                    builder.WithMicrosoftMemoryCacheHandle();
                });



//  ���������ļ�����һ�������Ļ������

services.AddCacheManagerConfiguration(Configuration, cfg => cfg.WithMicrosoftLogging(l => l.AddConsole()));

services.AddCacheManager();



//Ϊĳ������.���������ļ�����..ע���ǻ���Configuration��

   services.AddCacheManager<CacheSession>(Configuration,configure: builder =>
                {
                    builder.WithMicrosoftMemoryCacheHandle();
                });



���ڻ���Ϊʲôû��findall

https://github.com/MichaCo/CacheManager/issues/225




�����mysql

TODO:ʹ��Hangfire �洢���ݿ��������..��Ҫ��Ϊ�˱��ڼ�غ��������ݿ�������ֵ�����..�������Ե�..
//δʵ��.
�ϲ���ͬ����..
�����ʮ����..�洢һ��....�����ʮ����..�����δ洢�Ķ�����һ����..��ֻ�洢һ��

