import { configureStore } from '@reduxjs/toolkit';
import itineraryReducer from './ItinerarySlice';
import systemReducer from './SystemSlice';

export const store = configureStore({
  reducer: {
    itinerary: itineraryReducer,
    system: systemReducer
  }
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
