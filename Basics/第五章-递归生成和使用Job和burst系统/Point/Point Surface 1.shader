Shader "Graph/Point Surface" 
{
    // ����ɵ��ڵ����ԣ������û��� Unity �༭�����޸�
    Properties 
    {
        // ������һ����Ϊ _Smoothness �ĸ�������Χ��0 �� 1�������ڿ��Ʋ��ʵĹ⻬��
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
    }

    // ���� SubShader����������Ⱦ�߼�
    SubShader 
    {
        // ����ʹ�� Cg/HLSL �����д�Ĵ����
        CGPROGRAM

        // ���� Surface Shader��ʹ�ñ�׼��Ⱦ·����֧��ȫ��ǰ����Ӱ
        #pragma surface ConfigureSurface Standard fullforwardshadows

        // ָ�� Shader �ı���Ŀ��Ϊ 3.0����֧�ָ���ͼ������
        #pragma target 3.0

        // ��������ṹ�� Input���������Ⱦ�����н�������
        struct Input 
        {
            // �������������λ�ã�����Ϊ float3��3D ������
            float3 worldPos;
        };

        // ����һ����Ϊ _Smoothness �ĸ����������ڿ��Ʊ���Ĺ⻬��
        float _Smoothness;

        // ���ĺ��� ConfigureSurface���������ñ�����Ӿ�Ч��
        void ConfigureSurface (Input input, inout SurfaceOutputStandard surface)
        {
            // ����������ɫ��Albedo����ͨ������������ת��Ϊ��Χ [0, 1]
            // saturate() ����ȷ������������� [0, 1] ��Χ�ڣ���ֹ���
            surface.Albedo = saturate(input.worldPos * 0.5 + 0.5);

            // ʹ�� _Smoothness �������ñ���Ĺ⻬��
            surface.Smoothness = _Smoothness;
        }

        // ���� CG �����
        ENDCG
    }
    
    // �� Shader ������ʱ�����˵� "Diffuse" Shader
    FallBack "Diffuse"
}
