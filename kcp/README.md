1.���ù�����  ����Ըı�Ĭ�ϵ�ƽ̨..
��ѡ��anycpu��ʱ��,c#��Ŀ����ݵ�ǰϵͳ���ɳ���
��c++��ĿĬ����win32
release�����˵��ҵ����ù����������޸�.debug��release�����޸�


2.��dll����
2015�汾
https://msdn.microsoft.com/zh-cn/library/z4zxe9k8.aspx
2017�汾
https://docs.microsoft.com/zh-cn/cpp/build/exporting-from-a-dll

https://docs.microsoft.com/zh-cn/dotnet/framework/interop/consuming-unmanaged-dll-functions
3.����Ҳ�����ڵ�,���½���def�ļ�


4.���ڵ���
pdb��lib�ǵ�copy��ȥ

dll����
��dll��Ŀ��ִ��exe
https://docs.microsoft.com/zh-cn/visualstudio/debugger/how-to-debug-from-a-dll-project

��ϵ���
nercore��Ҫ��
add the following to your profile in launchsettings.json
"nativeDebugging" : true
https://docs.microsoft.com/zh-cn/visualstudio/debugger/how-to-debug-in-mixed-mode