import { configureStore } from '@reduxjs/toolkit';
import itineraryReducer from './itinerarySlice';

export const store = configureStore({
  reducer: {
    itinerary: itineraryReducer
  }
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
