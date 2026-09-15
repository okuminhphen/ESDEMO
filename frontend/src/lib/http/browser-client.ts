"use client";

import axios, { type AxiosRequestConfig } from "axios";
import { toApiError } from "@/lib/http/api-error";

const browserClient = axios.create({
  baseURL: "/api",
  headers: { "Content-Type": "application/json" },
  timeout: 10_000,
  withCredentials: true,
});

export async function browserRequest<T>(config: AxiosRequestConfig) {
  try {
    const response = await browserClient.request<T>(config);
    return response.data;
  } catch (error) {
    throw toApiError(error);
  }
}
