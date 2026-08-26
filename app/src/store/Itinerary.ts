import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { Itinerary } from '../models/Itinerary';

export interface ItineraryState {
  presentationData: Itinerary | null;
  activeDayIndex: number;
}

const initialState: ItineraryState = {
  presentationData: null,
  activeDayIndex: 0
};

const itinerarySlice = createSlice({
  name: 'itinerary',
  initialState,
  reducers: {
    setPresentationData(state, action: PayloadAction<Itinerary>) {
      state.presentationData = action.payload;
      state.activeDayIndex = 0;
    },
    clearPresentationData(state) {
      state.presentationData = null;
      state.activeDayIndex = 0;
    },
    setActiveDayIndex(state, action: PayloadAction<number>) {
      state.activeDayIndex = Math.max(0, action.payload);
    }
  }
});

export const { setPresentationData, clearPresentationData, setActiveDayIndex } = itinerarySlice.actions;
export default itinerarySlice.reducer;
