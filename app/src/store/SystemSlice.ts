import { createSlice, PayloadAction } from '@reduxjs/toolkit';

interface SystemState {
    message: string | null;
    processing: boolean;
}

const initialState: SystemState = {
    message: null,
    processing: false
};

const systemSlice = createSlice({
    name: 'system',
    initialState,
    reducers: {
        setSystemState(
            state,
            action: PayloadAction<SystemState>
        ) {
            state.message = action.payload.message;
            state.processing = action.payload.processing;
        },

        clearSystemState(state) {
            state.message = null;
            state.processing = false;
        }
    }
});

export const {
    setSystemState,
    clearSystemState
} = systemSlice.actions;

export default systemSlice.reducer;