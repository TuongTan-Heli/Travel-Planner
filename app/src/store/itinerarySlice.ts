import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { Itinerary } from '../models/itinerary';

export interface ItineraryState {
  presentationData: Itinerary | null;
}

const initialState: ItineraryState = {
  presentationData: null
};

const itinerarySlice = createSlice({
  name: 'itinerary',
  initialState,
  reducers: {
    setPresentationData(state, action: PayloadAction<Itinerary>) {
      state.presentationData = action.payload;
    },
    clearPresentationData(state) {
      state.presentationData = null;
    }
  }
});

export const { setPresentationData, clearPresentationData } = itinerarySlice.actions;
export default itinerarySlice.reducer;
