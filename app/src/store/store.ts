import { configureStore } from '@reduxjs/toolkit';
import itineraryReducer from './itinerarySlice';
import systemReducer from './systemSlice';

export const store = configureStore({
  reducer: {
    itinerary: itineraryReducer,
    system: systemReducer
  }
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
