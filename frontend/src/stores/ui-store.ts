"use client";

import { create } from "zustand";

type UiStore = {
  isAdminSidebarOpen: boolean;
  setAdminSidebarOpen: (isOpen: boolean) => void;
  toggleAdminSidebar: () => void;
};

export const useUiStore = create<UiStore>((set) => ({
  isAdminSidebarOpen: false,
  setAdminSidebarOpen: (isAdminSidebarOpen) => set({ isAdminSidebarOpen }),
  toggleAdminSidebar: () => set((state) => ({ isAdminSidebarOpen: !state.isAdminSidebarOpen })),
}));
