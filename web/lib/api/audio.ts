import axios from 'axios';

export async function loadAudioBuffer(url: string, signal: AbortSignal): Promise<ArrayBuffer> {
  try {
    const response = await axios.get<ArrayBuffer>(url, { responseType: 'arraybuffer', signal });
    return response.data;
  } catch (error) {
    if (axios.isAxiosError(error) && error.response?.status === 404) throw new Error('缺少聲部素材');
    throw error;
  }
}
